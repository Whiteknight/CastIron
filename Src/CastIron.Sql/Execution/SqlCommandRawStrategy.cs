using System;

namespace CastIron.Sql.Execution
{
    public class SqlCommandRawStrategy
    {
        private readonly ISqlCommandRawCommand _command;

        public SqlCommandRawStrategy(ISqlCommandRawCommand command)
        {
            _command = command;
        }

        public void Execute(IExecutionContext context, int index)
        {
            context.StartAction(index, "Setup Command");
            using (var dbCommand = context.CreateCommand())
            {
                try
                {
                    if (!_command.SetupCommand(dbCommand))
                    {
                        context.MarkAborted();
                        return;
                    }

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
                    throw e.WrapAsSqlProblemException(dbCommand, dbCommand.CommandText, index);
                }
            }
        }
    }

    public class SqlCommandRawStrategy<T>
    {
        private readonly ISqlCommandRawCommand<T> _command;

        public SqlCommandRawStrategy(ISqlCommandRawCommand<T> command)
        {
            _command = command;
        }

        public T Execute(IExecutionContext context, int index)
        {
            context.StartAction(index, "Setup Command");
            using (var dbCommand = context.CreateCommand())
            {
                try
                {
                    if (!_command.SetupCommand(dbCommand))
                    {
                        context.MarkAborted();
                        return default(T);
                    }

                    context.StartAction(index, "Execute");
                    dbCommand.ExecuteNonQuery();

                    context.StartAction(index, "Map Results");
                    var resultSet = new SqlResultSet(dbCommand, null);
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
                    throw e.WrapAsSqlProblemException(dbCommand, dbCommand.CommandText, index);
                }
            }
        }
    }
}