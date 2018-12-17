using System;
using System.Data;
using System.Threading.Tasks;
using CastIron.Sql.Mapping;

namespace CastIron.Sql.Execution
{
    public class SqlCommandStrategy
    {
        private readonly ISqlCommandSimple _command;

        public SqlCommandStrategy(ISqlCommandSimple command)
        {
            _command = command;
        }

        public void Execute(IExecutionContext context, int index)
        {
            context.StartAction(index, "Setup Command");
            using (var dbCommand = context.CreateCommand())
            {
                if (!SetupCommand(dbCommand))
                {
                    context.MarkAborted();
                    return;
                }

                try
                {
                    context.StartAction(index, "Execute");
                    dbCommand.ExecuteNonQuery();
                }
                catch (SqlProblemException)
                {
                    context.MarkAborted();
                    throw;
                }
                catch (Exception e)
                {
                    context.MarkAborted();
                    throw e.WrapAsSqlProblemException(dbCommand, index);
                }
            }
        }

        public async Task ExecuteAsync(IExecutionContext context, int index)
        {
            context.StartAction(index, "Setup Command");
            using (var dbCommand = context.CreateAsyncCommand())
            {
                if (!SetupCommand(dbCommand.Command))
                {
                    context.MarkAborted();
                    return;
                }

                try
                {
                    context.StartAction(index, "Execute");
                    await dbCommand.ExecuteNonQueryAsync();
                }
                catch (SqlProblemException)
                {
                    context.MarkAborted();
                    throw;
                }
                catch (Exception e)
                {
                    context.MarkAborted();
                    throw e.WrapAsSqlProblemException(dbCommand.Command, index);
                }
            }
        }

        public bool SetupCommand(IDbCommand command)
        {
            var text = _command.GetSql();
            if (string.IsNullOrEmpty(text))
                return false;
            command.CommandText = text;
            command.CommandType = (_command is ISqlStoredProc) ? CommandType.StoredProcedure : CommandType.Text;
            if (_command is ISqlParameterized parameterized)
                parameterized.SetupParameters(command, command.Parameters);
            return true;
        }
    }

    public class SqlCommandStrategy<T>
    {
        private readonly ISqlCommandSimple<T> _command;

        public SqlCommandStrategy(ISqlCommandSimple<T> command)
        {
            _command = command;
        }

        public T Execute(IExecutionContext context, int index)
        {
            context.StartAction(index, "Setup Command");
            var text = _command.GetSql();
            if (string.IsNullOrEmpty(text))
                return default(T);

            using (var dbCommand = context.CreateCommand())
            {
                dbCommand.CommandText = text;
                try
                {
                    dbCommand.CommandType = (_command is ISqlStoredProc) ? CommandType.StoredProcedure : CommandType.Text;
                    if (_command is ISqlParameterized parameterized)
                        parameterized.SetupParameters(dbCommand, dbCommand.Parameters);

                    context.StartAction(index, "Execute");
                    dbCommand.ExecuteNonQuery();

                    context.StartAction(index, "Map Results");
                    var resultSet = new SqlDataReaderResult(dbCommand, context, null);
                    return _command.ReadOutputs(resultSet);
                }
                catch (SqlProblemException)
                {
                    context.MarkAborted();
                    throw;
                }
                catch (Exception e)
                {
                    context.MarkAborted();
                    throw e.WrapAsSqlProblemException(dbCommand, index);
                }
            }
        }

        public async Task<T> ExecuteAsync(IExecutionContext context, int index)
        {
            context.StartAction(index, "Setup Command");
            var text = _command.GetSql();
            if (string.IsNullOrEmpty(text))
                return default(T);

            using (var dbCommand = context.CreateAsyncCommand())
            {
                dbCommand.Command.CommandText = text;
                try
                {
                    dbCommand.Command.CommandType = (_command is ISqlStoredProc) ? CommandType.StoredProcedure : CommandType.Text;
                    if (_command is ISqlParameterized parameterized)
                        parameterized.SetupParameters(dbCommand.Command, dbCommand.Command.Parameters);

                    context.StartAction(index, "Execute");
                    await dbCommand.ExecuteNonQueryAsync();

                    context.StartAction(index, "Map Results");
                    var resultSet = new SqlDataReaderResult(dbCommand.Command, context, null);
                    return await Task.Run(() => _command.ReadOutputs(resultSet));
                }
                catch (SqlProblemException)
                {
                    context.MarkAborted();
                    throw;
                }
                catch (Exception e)
                {
                    context.MarkAborted();
                    throw e.WrapAsSqlProblemException(dbCommand.Command, index);
                }
            }
        }
    }
}