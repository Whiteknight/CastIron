using System;
using System.Threading.Tasks;

namespace CastIron.Sql.Execution
{
    public class SqlConnectionAccessorStrategy
    {
        public void Execute(ISqlConnectionAccessor accessor, IExecutionContext context, int index)
        {
            try
            {
                context.StartAction(index, "Execute");
                accessor.Execute(context.Connection, context.Transaction);
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

        public async Task ExecuteAsync(ISqlConnectionAccessor accessor, IExecutionContext context, int index)
        {
            try
            {
                context.StartAction(index, "Execute");
                await Task.Run(() => accessor.Execute(context.Connection, context.Transaction));
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

        public T Execute<T>(ISqlConnectionAccessor<T> accessor, IExecutionContext context, int index)
        {
            try
            {
                context.StartAction(index, "Execute");
                return accessor.Query(context.Connection, context.Transaction);
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

        public async Task<T> ExecuteAsync<T>(ISqlConnectionAccessor<T> accessor, IExecutionContext context, int index)
        {
            try
            {
                context.StartAction(index, "Execute");
                return await Task.Run(() => accessor.Query(context.Connection, context.Transaction));
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