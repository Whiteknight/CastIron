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

        public ISqlResult<T> Add<T>(ISqlQuery<T> query)
        {
            var result = new SqlResult<T>();
            _executors.Add((c, i) => result.SetValue(new SqlQueryStrategy<T>(query).Execute(c, i)));
            return result;
        }

        public ISqlResult Add(ISqlCommand command)
        {
            var result = new SqlResult();
            _executors.Add((c, i) =>
            {
                new SqlCommandStrategy(command).Execute(c, i);
                result.IsComplete = true;
            });
            return result;
        }

        public IReadOnlyList<Action<IExecutionContext, int>> GetExecutors()
        {
            return _executors;
        }

        // TODO: More methods to add different ISql* query and command variants
    }
}