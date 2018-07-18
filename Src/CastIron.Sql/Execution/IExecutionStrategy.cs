using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace CastIron.Sql.Execution
{
    public interface IExecutionContext : IDisposable
    {
        IDbConnection Connection { get; }

        IDbTransaction Transaction { get; }

        IDbCommand CreateCommand();
    }

    public class ExecutionContext : IExecutionContext
    {
        private readonly Dictionary<int, SqlProblemException> _exceptions;
        private readonly List<Action<IExecutionContext, int>> _executors;

        public ExecutionContext(IDbConnection connection, IDbTransaction transaction)
        {
            Connection = connection;
            Transaction = transaction;
            _exceptions = new Dictionary<int, SqlProblemException>();
            _executors = new List<Action<IExecutionContext, int>>();
        }

        public IDbConnection Connection { get; }
        public IDbTransaction Transaction { get; }

        public IDbCommand CreateCommand()
        {
            var command = Connection.CreateCommand();
            if (Transaction != null)
                command.Transaction = Transaction;
            return command;
        }

        public Exception GetException()
        {
            if (_exceptions.Count == 0)
                return null;
            if (_exceptions.Count == 1)
                return _exceptions.Values.Single();
            return new AggregateException(_exceptions.Values);
        }

        public void ThrowExceptionIfAny()
        {
            var e = GetException();
            if (e != null)
                throw e;
        }

        public void AddExecutor(Action<IExecutionContext, int> executor)
        {
            _executors.Add(executor);
        }

        public void Dispose()
        {
            Connection?.Dispose();
            Transaction?.Commit();
            Transaction?.Dispose();
        }
    }

    public class SqlQueryStrategy<T>
    {
        private readonly ISqlQuery<T> _query;

        public SqlQueryStrategy(ISqlQuery<T> query)
        {
            _query = query;
        }

        public T Execute(IExecutionContext context, int index)
        {
            var text = _query.GetSql();
            if (string.IsNullOrEmpty(text))
                return default(T);

            using (var command = context.CreateCommand())
            {
                try
                {
                    command.CommandText = text;
                    command.CommandType = (_query is ISqlStoredProc) ? CommandType.StoredProcedure : CommandType.Text;
                    command.MaybeAddParameters(_query);
                    using (var reader = command.ExecuteReader())
                    {
                        var rawResultSet = new SqlQueryRawResultSet(command, reader);
                        return _query.Read(rawResultSet);
                    }
                }
                catch (SqlProblemException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    throw e.WrapAsSqlProblemException(command, text, index);
                }
            }
        }
    }

    public class SqlQueryRawCommandStrategy<T>
    {
        private readonly ISqlQueryRawCommand<T> _query;

        public SqlQueryRawCommandStrategy(ISqlQueryRawCommand<T> query)
        {
            _query = query;
        }

        public T Execute(IExecutionContext context, int index)
        {
            using (var command = context.CreateCommand())
            {
                if (!_query.SetupCommand(command))
                    return default(T);

                try
                {
                    using (var reader = command.ExecuteReader())
                    {
                        var resultSet = new SqlQueryRawResultSet(command, reader);
                        return _query.Read(resultSet);
                    }
                }
                catch (SqlProblemException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    throw e.WrapAsSqlProblemException(command, command.CommandText, index);
                }
            }
        }
    }

    public class SqlQueryRawConnectionStrategy<T> 
    {
        private readonly ISqlQueryRawConnection<T> _query;

        public SqlQueryRawConnectionStrategy(ISqlQueryRawConnection<T> query)
        {
            _query = query;
        }

        public T Execute(IExecutionContext context, int index)
        {
            try
            {
                return _query.Query(context.Connection, context.Transaction);
            }
            catch (SqlProblemException)
            {
                throw;
            }
            catch (Exception e)
            {
                // We can't do anything fancy with error handling, because we don't know what the user
                // is trying to do
                throw e.WrapAsSqlProblemException(null, null, index);
            }
        }
    }

    public class SqlCommandStrategy
    {
        private readonly ISqlCommand _command;

        public SqlCommandStrategy(ISqlCommand command)
        {
            _command = command;
        }

        public void Execute(IExecutionContext context, int index)
        {
            var text = _command.GetSql();
            if (string.IsNullOrEmpty(text))
                return;

            using (var dbCommand = context.CreateCommand())
            {
                try
                {
                    dbCommand.CommandText = text;
                    dbCommand.CommandType = (_command is ISqlStoredProc) ? CommandType.StoredProcedure : CommandType.Text;
                    dbCommand.MaybeAddParameters(_command);
                    dbCommand.ExecuteNonQuery();
                }
                catch (SqlProblemException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    throw e.WrapAsSqlProblemException(dbCommand, text, index);
                }
            }
        }
    }

    public class SqlCommandStrategy<T>
    {
        private readonly ISqlCommand<T> _command;

        public SqlCommandStrategy(ISqlCommand<T> command)
        {
            _command = command;
        }

        public T Execute(IExecutionContext context, int index)
        {
            var text = _command.GetSql();
            if (string.IsNullOrEmpty(text))
                return default(T);

            using (var dbCommand = context.CreateCommand())
            {
                try
                {
                    dbCommand.CommandText = text;
                    dbCommand.CommandType = (_command is ISqlStoredProc) ? CommandType.StoredProcedure : CommandType.Text;
                    dbCommand.MaybeAddParameters(_command);
                    dbCommand.ExecuteNonQuery();
                    var resultSet = new SqlQueryRawResultSet(dbCommand, null);
                    return _command.ReadOutputs(resultSet);
                }
                catch (SqlProblemException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    throw e.WrapAsSqlProblemException(dbCommand, text, index);
                }
            }
        }
    }

    public class SqlCommandRawStrategy
    {
        private readonly ISqlCommandRaw _command;

        public SqlCommandRawStrategy(ISqlCommandRaw command)
        {
            _command = command;
        }

        public void Execute(IExecutionContext context, int index)
        {
            using (var dbCommand = context.CreateCommand())
            {
                try
                {
                    if (!_command.SetupCommand(dbCommand))
                        return;

                    dbCommand.ExecuteNonQuery();

                }
                catch (SqlProblemException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    throw e.WrapAsSqlProblemException(dbCommand, dbCommand.CommandText, index);
                }
            }
        }
    }

    public class SqlCommandRawStrategy<T>
    {
        private readonly ISqlCommandRaw<T> _command;

        public SqlCommandRawStrategy(ISqlCommandRaw<T> command)
        {
            _command = command;
        }

        public T Execute(IExecutionContext context, int index)
        {
            using (var dbCommand = context.CreateCommand())
            {
                try
                {
                    if (!_command.SetupCommand(dbCommand))
                        return default(T);

                    dbCommand.ExecuteNonQuery();
                    var resultSet = new SqlQueryRawResultSet(dbCommand, null);
                    return _command.ReadOutputs(resultSet);
                }
                catch (SqlProblemException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    throw e.WrapAsSqlProblemException(dbCommand, dbCommand.CommandText, index);
                }
            }
        }
    }

    public static class DbCommandExtensions
    {
        public static void MaybeAddParameters(this IDbCommand command, object obj)
        {
            if (obj is ISqlParameterized withParams)
            {
                var parameters = withParams.GetParameters();
                foreach (var parameter in parameters)
                    AddParameter(command, parameter);
            }
        }

        private static void AddParameter(IDbCommand command, Parameter parameter)
        {
            var param = command.CreateParameter();
            param.ParameterName = parameter.Name;
            param.Value = parameter.Value;
            param.DbType = parameter.Type;
            param.Size = parameter.Size;
            param.Direction = parameter.Direction;
            command.Parameters.Add(param);
        }
    }

    public static class ExceptionExtensions
    {
        public static SqlProblemException WrapAsSqlProblemException(this Exception e, IDbCommand command, string text, int index = -1)
        {
            var sb = new StringBuilder();
            if (command != null)
            {
                for (int i = 0; i < command.Parameters.Count; i++)
                {
                    if (!(command.Parameters[i] is SqlParameter param))
                        continue;
                    sb.Append("--DECLARE ");
                    sb.Append(param.ParameterName);
                    sb.Append(" ");
                    sb.Append(param.DbType);
                    sb.Append(" = ");
                    sb.Append(param.SqlValue);
                    sb.AppendLine(";");
                }
                sb.AppendLine();
            }

            if (!string.IsNullOrEmpty(text))
                sb.AppendLine(text);

            var message = e.Message;
            if (index >= 0)
                message = $"Error executing statement {index}\n{e.Message}";
            return new SqlProblemException(message, sb.ToString(), e);
        }
    }
}
