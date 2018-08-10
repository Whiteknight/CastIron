using System;
using System.Data;

namespace CastIron.Sql.Execution
{
    public class SqlCommandRawStrategy
    {
        private readonly ISqlCommand _command;

        public SqlCommandRawStrategy(ISqlCommand command)
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
                    if (!SetupCommand(dbCommand))
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
                    throw e.WrapAsSqlProblemException(dbCommand, index);
                }
            }
        }

        public bool SetupCommand(IDbCommand command)
        {
            return _command.SetupCommand(command);
        }
    }

    public class SqlCommandRawStrategy<T>
    {
        private readonly ISqlCommand<T> _command;

        public SqlCommandRawStrategy(ISqlCommand<T> command)
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
                    if (!SetupCommand(dbCommand))
                    {
                        context.MarkAborted();
                        return default(T);
                    }

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

        public bool SetupCommand(IDbCommand command)
        {
            return _command.SetupCommand(command);
        }
    }
}