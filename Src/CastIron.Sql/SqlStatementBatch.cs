using System;
using System.Collections.Generic;
using CastIron.Sql.Execution;
using CastIron.Sql.Statements;

namespace CastIron.Sql
{
    // TODO: Cross-thread support. I should be able to dispatch executors to a separate thread and wait for the
    // query to be batched and executed in a thread-safe manner. I should be able to .Add() from multiple threads
    /// <summary>
    /// Batch of statements to all be executed together on a single open connection.
    /// </summary>
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

        public ISqlResultPromise<T> Add<T>(ISqlQuery<T> query)
        {
            var result = new SqlResultPromise<T>();
            AddExecutor((c, i) => result.SetValue(new SqlQueryStrategy<T>(query).Execute(c, i)));
            return result;
        }

        public ISqlResultPromise<T> Add<T>(ISqlQueryRawCommand<T> query)
        {
            var result = new SqlResultPromise<T>();
            AddExecutor((c, i) => result.SetValue(new SqlQueryRawCommandStrategy<T>(query).Execute(c, i)));
            return result;
        }

        public ISqlResultPromise<T> Add<T>(ISqlQueryRawConnection<T> query)
        {
            var result = new SqlResultPromise<T>();
            AddExecutor((c, i) => result.SetValue(new SqlQueryRawConnectionStrategy<T>(query).Execute(c, i)));
            return result;
        }

        public ISqlResultPromise Add(ISqlCommand command)
        {
            var result = new SqlResultPromise();
            AddExecutor((c, i) =>
            {
                new SqlCommandStrategy(command).Execute(c, i);
                result.IsComplete = true;
            });
            return result;
        }

        public ISqlResultPromise Add(ISqlCommandRawCommand command)
        {
            var result = new SqlResultPromise();
            AddExecutor((c, i) =>
            {
                new SqlCommandRawStrategy(command).Execute(c, i);
                result.IsComplete = true;
            });
            return result;
        }

        public ISqlResultPromise<T> Add<T>(ISqlCommandRawCommand<T> command)
        {
            var result = new SqlResultPromise<T>();
            AddExecutor((c, i) => result.SetValue(new SqlCommandRawStrategy<T>(command).Execute(c, i)));
            return result;
        }

        public IReadOnlyList<Action<IExecutionContext, int>> GetExecutors()
        {
            return _executors;
        }

        public ISqlResultPromise Add(string sql)
        {
            return Add(new SqlCommand(sql));
        }

        public ISqlResultPromise<IReadOnlyList<T>> Add<T>(string sql)
        {
            return Add(new SqlQuery<T>(sql));
        }
    }
}