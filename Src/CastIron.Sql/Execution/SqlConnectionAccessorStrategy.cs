using System;

namespace CastIron.Sql.Execution
{
    public class SqlConnectionAccessorStrategy
    {
        public void Execute(ISqlConnectionAccessor accessor, IExecutionContext context, int index)
        {
            try
            {
                context.StartAction(index, "Execute");
                accessor.Execute(context.Connection.Connection, context.Transaction);
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
                throw SqlProblemException.Wrap(e, null, index);
            }
        }

        public T Execute<T>(ISqlConnectionAccessor<T> accessor, IExecutionContext context, int index)
        {
            try
            {
                context.StartAction(index, "Execute");
                return accessor.Query(context.Connection.Connection, context.Transaction);
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
                throw SqlProblemException.Wrap(e, null, index);
            }
        }
    }
}