using System;

namespace CastIron.Sql.Execution
{
    public class SqlConnectionAccessorStrategy<T> 
    {
        private readonly ISqlConnectionAccessor<T> _accessor;

        public SqlConnectionAccessorStrategy(ISqlConnectionAccessor<T> accessor)
        {
            _accessor = accessor;
        }

        public T Execute(IExecutionContext context, int index)
        {
            try
            {
                context.StartAction(index, "Execute");
                return _accessor.Query(context.Connection, context.Transaction);
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

    public class SqlConnectionAccessorStrategy
    {
        private readonly ISqlConnectionAccessor _accessor;

        public SqlConnectionAccessorStrategy(ISqlConnectionAccessor accessor)
        {
            _accessor = accessor;
        }

        public void Execute(IExecutionContext context, int index)
        {
            try
            {
                context.StartAction(index, "Execute");
                _accessor.Execute(context.Connection, context.Transaction);
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