using System;
using System.Data;

namespace CastIron.Sql.Execution
{
    public class SqlQueryRawCommandStrategy<T>
    {
        private readonly ISqlQueryRawCommand<T> _query;

        public SqlQueryRawCommandStrategy(ISqlQueryRawCommand<T> query)
        {
            _query = query;
        }

        public T Execute(IExecutionContext context, int index)
        {
            context.StartAction(index, "Setup Command");
            using (var command = context.CreateCommand())
            {
                if (!SetupCommand(command))
                {
                    context.MarkAborted();
                    return default(T);
                }

                try
                {
                    context.StartAction(index, "Execute");
                    using (var reader = command.ExecuteReader())
                    {
                        context.StartAction(index, "Map Results");
                        var resultSet = new SqlResultSet(command, context, reader);
                        return _query.Read(resultSet);
                    }
                }
                catch (SqlProblemException)
                {
                    context.MarkAborted();
                    throw;
                }
                catch (Exception e)
                {
                    context.MarkAborted();
                    throw e.WrapAsSqlProblemException(command, index);
                }
            }
        }

        public bool SetupCommand(IDbCommand command)
        {
            return _query.SetupCommand(command);
        }
    }
}