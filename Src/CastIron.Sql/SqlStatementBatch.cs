using System;
using System.Collections.Generic;
using CastIron.Sql.Execution;

namespace CastIron.Sql
{
    // TODO: Cross-thread support. I should be able to dispatch executors to a separate thread and wait for the
    // query to be batched and executed in a thread-safe manner.
    public class SqlStatementBatch
    {
        private readonly List<Action<IExecutionContext, int>> _executors;

        public SqlStatementBatch()
        {
            _executors = new List<Action<IExecutionContext, int>>();
        }

        private void AddExecutor(Action<IExecutionContext, int> executor)
        {
            // TODO: Make this thread safe.
            // TODO: When we start to read the list of executors, it cannot be modified anymore.
            _executors.Add(executor);
        }

        public ISqlResult<T> Add<T>(ISqlQuery<T> query)
        {
            var result = new SqlResult<T>();
            AddExecutor((c, i) => result.SetValue(new SqlQueryStrategy<T>(query).Execute(c, i)));
            return result;
        }

        public ISqlResult<T> Add<T>(ISqlQueryRawCommand<T> query)
        {
            var result = new SqlResult<T>();
            AddExecutor((c, i) => result.SetValue(new SqlQueryRawCommandStrategy<T>(query).Execute(c, i)));
            return result;
        }

        public ISqlResult<T> Add<T>(ISqlQueryRawConnection<T> query)
        {
            var result = new SqlResult<T>();
            AddExecutor((c, i) => result.SetValue(new SqlQueryRawConnectionStrategy<T>(query).Execute(c, i)));
            return result;
        }

        public ISqlResult Add(ISqlCommand command)
        {
            var result = new SqlResult();
            AddExecutor((c, i) =>
            {
                new SqlCommandStrategy(command).Execute(c, i);
                result.IsComplete = true;
            });
            return result;
        }

        public ISqlResult Add(ISqlCommandRawCommand command)
        {
            var result = new SqlResult();
            AddExecutor((c, i) =>
            {
                new SqlCommandRawStrategy(command).Execute(c, i);
                result.IsComplete = true;
            });
            return result;
        }

        public ISqlResult<T> Add<T>(ISqlCommandRawCommand<T> command)
        {
            var result = new SqlResult<T>();
            AddExecutor((c, i) => result.SetValue(new SqlCommandRawStrategy<T>(command).Execute(c, i)));
            return result;
        }

        public IReadOnlyList<Action<IExecutionContext, int>> GetExecutors()
        {
            return _executors;
        }
    }
}