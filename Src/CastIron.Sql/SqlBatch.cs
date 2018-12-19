using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using CastIron.Sql.Execution;
using CastIron.Sql.Statements;

namespace CastIron.Sql
{
    // TODO: Do we need an ISqlBatch abstraction or is this general-purpose enough?
    /// <summary>
    /// Batch of statements to all be executed together on a single open connection.
    /// </summary>
    public class SqlBatch
    {
        private readonly IDataInteractionFactory _interactionFactory;
        private readonly ConcurrentQueue<Action<IExecutionContext, int>> _executors;
        private int _beingRead;

        public SqlBatch(IDataInteractionFactory interactionFactory)
        {
            _interactionFactory = interactionFactory;
            _beingRead = 0;
            _executors = new ConcurrentQueue<Action<IExecutionContext, int>>();
        }

        public IReadOnlyList<Action<IExecutionContext, int>> GetExecutors()
        {
            var beingRead = Interlocked.CompareExchange(ref _beingRead, 1, 0) == 0;
            return beingRead ? _executors.ToArray() : new Action<IExecutionContext, int>[0];
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
            var promise = new SqlResultPromise<T>();
            AddExecutor((c, i) =>
            {
                var result = new SqlQuerySimpleStrategy().Execute(query, c, i);
                promise.SetValue(result);
            });
            return promise;
        }

        public ISqlResultPromise<T> Add<T>(ISqlQuery<T> query)
        {
            var promise = new SqlResultPromise<T>();
            AddExecutor((c, i) =>
            {
                var result = new SqlQueryStrategy(_interactionFactory).Execute(query, c, i);
                promise.SetValue(result);
            });
            return promise;
        }

        public ISqlResultPromise<T> Add<T>(ISqlConnectionAccessor<T> accessor)
        {
            var promise = new SqlResultPromise<T>();
            AddExecutor((c, i) =>
            {
                var result = new SqlConnectionAccessorStrategy().Execute(accessor, c, i);
                promise.SetValue(result);
            });
            return promise;
        }

        public ISqlResultPromise Add(ISqlCommandSimple command)
        {
            var result = new SqlResultPromise();
            AddExecutor((c, i) =>
            {
                new SqlCommandSimpleStrategy().Execute(command, c, i);
                result.SetComplete();
            });
            return result;
        }

        public ISqlResultPromise Add(ISqlCommand command)
        {
            var result = new SqlResultPromise();
            AddExecutor((c, i) =>
            {
                new SqlCommandStrategy(_interactionFactory).Execute(command, c, i);
                result.SetComplete();
            });
            return result;
        }

        public ISqlResultPromise<T> Add<T>(ISqlCommand<T> command)
        {
            var promise = new SqlResultPromise<T>();
            AddExecutor((c, i) =>
            {
                var result = new SqlCommandStrategy(_interactionFactory).Execute(command, c, i);
                promise.SetValue(result);
            });
            return promise;
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