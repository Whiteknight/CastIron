using System;

namespace CastIron.Sql.Execution
{
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
}