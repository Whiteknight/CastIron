using System;
using System.Data;

namespace CastIron.Sql.Execution
{
    public class SqlCommandStrategy
    {
        private readonly ISqlCommand _command;

        public SqlCommandStrategy(ISqlCommand command)
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

        public bool SetupCommand(IDbCommand command)
        {
            var text = _command.GetSql();
            if (string.IsNullOrEmpty(text))
                return false;
            command.CommandText = text;
            command.CommandType = (_command is ISqlStoredProc) ? CommandType.StoredProcedure : CommandType.Text;
            if (_command is ISqlParameterized parameterized)
                parameterized.SetupParameters(command.Parameters);
            return true;
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
                        parameterized.SetupParameters(dbCommand.Parameters);

                    context.StartAction(index, "Execute");
                    dbCommand.ExecuteNonQuery();

                    context.StartAction(index, "Map Results");
                    var resultSet = new SqlResultSet(dbCommand, context, null);
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
    }
}