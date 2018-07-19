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
        private readonly ISqlCommandRawCommand<T> _command;

        public SqlCommandRawStrategy(ISqlCommandRawCommand<T> command)
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
                    var resultSet = new SqlResultSet(dbCommand, null);
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
}