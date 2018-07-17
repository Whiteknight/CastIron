using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace CastIron.Sql.Execution
{
    public interface IExecutionStrategy
    {
        object Execute(IDbConnection connection, int index);
    }

    public class SqlQueryStratgy<T> : IExecutionStrategy
    {
        private readonly ISqlQuery<T> _query;

        public SqlQueryStratgy(ISqlQuery<T> query)
        {
            _query = query;
        }

        public object Execute(IDbConnection connection, int index)
        {
            var text = _query.GetSql();
            if (string.IsNullOrEmpty(text))
                return default(T);

            using (var command = connection.CreateCommand())
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
                    e.ThrowAsSqlProblemException(command, text, index);
                }
            }

            return default(T);
        }
    }

    public class SqlQueryRawCommandStrategy<T> : IExecutionStrategy
    {
        private readonly ISqlQueryRawCommand<T> _query;

        public SqlQueryRawCommandStrategy(ISqlQueryRawCommand<T> query)
        {
            _query = query;
        }


        public object Execute(IDbConnection connection, int index)
        {
            using (var command = connection.CreateCommand())
            {
                if (!_query.SetupCommand(command))
                    return default(T);
                try
                {
                    using (var reader = command.ExecuteReader())
                    {
                        var result = new SqlQueryRawResultSet(command, reader);
                        return _query.Read(result);
                    }
                }
                catch (SqlProblemException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    e.ThrowAsSqlProblemException(command, command.CommandText, index);
                }
            }

            return default(T);
        }
    }

    public class SqlQueryRawConnectionStrategy<T> : IExecutionStrategy
    {
        private readonly ISqlQueryRawConnection<T> _query;

        public SqlQueryRawConnectionStrategy(ISqlQueryRawConnection<T> query)
        {
            _query = query;
        }


        public object Execute(IDbConnection connection, int index)
        {
            return _query.Query(connection);
            // We can't do anything fancy with error handling, because we don't know what the user
            // is trying to do
        }
    }

    public class SqlCommandStrategy : IExecutionStrategy
    {
        private readonly ISqlCommand _command;

        public SqlCommandStrategy(ISqlCommand command)
        {
            _command = command;
        }


        public object Execute(IDbConnection connection, int index)
        {
            var text = _command.GetSql();
            if (string.IsNullOrEmpty(text))
                return null;

            try
            {
                using (var dbCommand = connection.CreateCommand())
                {
                    dbCommand.CommandText = text;
                    dbCommand.CommandType = (_command is ISqlStoredProc) ? CommandType.StoredProcedure : CommandType.Text;
                    dbCommand.MaybeAddParameters(_command);
                    dbCommand.ExecuteNonQuery();
                    return null;
                }
            }
            catch (SqlProblemException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new SqlProblemException(e.Message, text, e);
            }
        }
    }

    public class SqlCommandStrategy<T> : IExecutionStrategy
    {
        private readonly ISqlCommand<T> _command;

        public SqlCommandStrategy(ISqlCommand<T> command)
        {
            _command = command;
        }


        public object Execute(IDbConnection connection, int index)
        {
            var text = _command.GetSql();
            if (string.IsNullOrEmpty(text))
                return null;

            try
            {
                using (var dbCommand = connection.CreateCommand())
                {
                    dbCommand.CommandText = text;
                    dbCommand.CommandType = (_command is ISqlStoredProc) ? CommandType.StoredProcedure : CommandType.Text;
                    dbCommand.MaybeAddParameters(_command);
                    dbCommand.ExecuteNonQuery();
                    var result = new SqlQueryRawResultSet(dbCommand, null);
                    return _command.ReadOutputs(result);
                }
            }
            catch (SqlProblemException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new SqlProblemException(e.Message, text, e);
            }
        }
    }

    public class SqlCommandRawStrategy : IExecutionStrategy
    {
        private readonly ISqlCommandRaw _command;

        public SqlCommandRawStrategy(ISqlCommandRaw command)
        {
            _command = command;
        }


        public object Execute(IDbConnection connection, int index)
        {
            try
            {
                using (var dbCommand = connection.CreateCommand())
                {
                    if (!_command.SetupCommand(dbCommand))
                        return null;
                    
                    dbCommand.ExecuteNonQuery();
                    return null;
                }
            }
            catch (SqlProblemException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new SqlProblemException(e.Message, "", e);
            }
        }
    }

    public class SqlCommandRawStrategy<T> : IExecutionStrategy
    {
        private readonly ISqlCommandRaw<T> _command;

        public SqlCommandRawStrategy(ISqlCommandRaw<T> command)
        {
            _command = command;
        }


        public object Execute(IDbConnection connection, int index)
        {
            try
            {
                using (var dbCommand = connection.CreateCommand())
                {
                    if (!_command.SetupCommand(dbCommand))
                        return null;

                    dbCommand.ExecuteNonQuery();
                    var result = new SqlQueryRawResultSet(dbCommand, null);
                    return _command.ReadOutputs(result);
                }
            }
            catch (SqlProblemException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new SqlProblemException(e.Message, "", e);
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
            param.Direction = parameter.Direction;
            command.Parameters.Add(param);
        }
    }

    public static class ExceptionExtensions
        {
            public static void ThrowAsSqlProblemException(this Exception e, IDbCommand command, string text, int index = -1)
            {
                var sb = new StringBuilder();
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
                sb.AppendLine(text);

                var message = e.Message;
                if (index >= 0)
                    message = $"Error executing statement {index}\n{e.Message}";
                throw new SqlProblemException(message, sb.ToString(), e);
            }
        }
}
