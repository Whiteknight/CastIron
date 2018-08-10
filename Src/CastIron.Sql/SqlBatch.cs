using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using CastIron.Sql.Execution;
using CastIron.Sql.Statements;

namespace CastIron.Sql
{
    /// <summary>
    /// Batch of statements to all be executed together on a single open connection.
    /// </summary>
    public class SqlBatch
    {
        private readonly ConcurrentQueue<Action<IExecutionContext, int>> _executors;
        private int _beingRead;

        public SqlBatch()
        {
            _beingRead = 0;
            _executors = new ConcurrentQueue<Action<IExecutionContext, int>>();
        }

        public IReadOnlyList<Action<IExecutionContext, int>> GetExecutors()
        {
            var beingRead = Interlocked.CompareExchange(ref _beingRead, 1, 0) == 0;
            if (beingRead)
                return _executors.ToArray();
            return new Action<IExecutionContext, int>[0];
        }

        private void AddExecutor(Action<IExecutionContext, int> executor)
        {
            var canAdd = Interlocked.CompareExchange(ref _beingRead, 0, 0) == 0;
            if (!canAdd)
                return;
            _executors.Enqueue(executor);
        }

        public ISqlResultPromise<T> Add<T>(ISqlQuerySimple<T> query)
        {
            var result = new SqlResultPromise<T>();
            AddExecutor((c, i) => result.SetValue(new SqlQueryStrategy<T>(query).Execute(c, i)));
            return result;
        }

        public ISqlResultPromise<T> Add<T>(ISqlQuery<T> query)
        {
            var result = new SqlResultPromise<T>();
            AddExecutor((c, i) => result.SetValue(new SqlQueryRawCommandStrategy<T>(query).Execute(c, i)));
            return result;
        }

        public ISqlResultPromise<T> Add<T>(ISqlConnectionAccessor<T> query)
        {
            var result = new SqlResultPromise<T>();
            AddExecutor((c, i) => result.SetValue(new SqlQueryRawConnectionStrategy<T>(query).Execute(c, i)));
            return result;
        }

        public ISqlResultPromise Add(ISqlCommandSimple command)
        {
            var result = new SqlResultPromise();
            AddExecutor((c, i) =>
            {
                new SqlCommandStrategy(command).Execute(c, i);
                result.IsComplete = true;
            });
            return result;
        }

        public ISqlResultPromise Add(ISqlCommand command)
        {
            var result = new SqlResultPromise();
            AddExecutor((c, i) =>
            {
                new SqlCommandRawStrategy(command).Execute(c, i);
                result.IsComplete = true;
            });
            return result;
        }

        public ISqlResultPromise<T> Add<T>(ISqlCommand<T> command)
        {
            var result = new SqlResultPromise<T>();
            AddExecutor((c, i) => result.SetValue(new SqlCommandRawStrategy<T>(command).Execute(c, i)));
            return result;
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