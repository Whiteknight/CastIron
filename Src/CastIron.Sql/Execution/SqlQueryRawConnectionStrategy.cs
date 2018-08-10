﻿using System;

namespace CastIron.Sql.Execution
{
    public class SqlQueryRawConnectionStrategy<T> 
    {
        private readonly ISqlConnectionAccessor<T> _query;

        public SqlQueryRawConnectionStrategy(ISqlConnectionAccessor<T> query)
        {
            _query = query;
        }

        public T Execute(IExecutionContext context, int index)
        {
            try
            {
                context.StartAction(index, "Execute");
                return _query.Query(context.Connection, context.Transaction);
            }
            catch (SqlProblemException)
            {
                context.MarkAborted();
                throw;
            }
            catch (Exception e)
            {
                context.MarkAborted();
                // We can't do anything fancy with error handling, because we don't know what the user
                // is trying to do
                throw e.WrapAsSqlProblemException(null, index);
            }
        }
    }
}