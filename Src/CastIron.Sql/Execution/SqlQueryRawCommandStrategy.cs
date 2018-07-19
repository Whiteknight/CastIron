using System;

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
            using (var command = context.CreateCommand())
            {
                if (!_query.SetupCommand(command))
                    return default(T);

                try
                {
                    using (var reader = command.ExecuteReader())
                    {
                        var resultSet = new SqlResultSet(command, reader);
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
}