using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using CastIron.Sql.Execution;
using CastIron.Sql.Statements;
using CastIron.Sql.Utility;

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

        public ISqlResultPromise<T> Add<T>(ISqlQuerySimple query, IResultMaterializer<T> reader)
        {
            Assert.ArgumentNotNull(query, nameof(query));
            Assert.ArgumentNotNull(reader, nameof(reader));
            var promise = new SqlResultPromise<T>();
            AddExecutor((c, i) =>
            {
                var result = new SqlQuerySimpleStrategy().Execute(query, reader, c, i);
                promise.SetValue(result);
            });
            return promise;
        }

        public ISqlResultPromise<T> Add<T>(ISqlQuerySimple<T> query)
        {
            return Add(query, query);
        }

        public ISqlResultPromise<T> Add<T>(ISqlQuery query, IResultMaterializer<T> reader)
        {
            Assert.ArgumentNotNull(query, nameof(query));
            Assert.ArgumentNotNull(reader, nameof(reader));
            var promise = new SqlResultPromise<T>();
            AddExecutor((c, i) =>
            {
                var result = new SqlQueryStrategy(_interactionFactory).Execute(query, reader, c, i);
                promise.SetValue(result);
            });
            return promise;
        }

        public ISqlResultPromise<T> Add<T>(ISqlQuery<T> query)
        {
            return Add(query, query);
        }

        public ISqlResultPromise Add(ISqlCommandSimple command)
        {
            Assert.ArgumentNotNull(command, nameof(command));
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
            Assert.ArgumentNotNull(command, nameof(command));
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
            Assert.ArgumentNotNull(command, nameof(command));
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
            Assert.ArgumentNotNullOrEmpty(sql, nameof(sql));
            return Add(new SqlCommand(sql));
        }

        public ISqlResultPromise<IReadOnlyList<T>> Add<T>(string sql)
        {
            Assert.ArgumentNotNullOrEmpty(sql, nameof(sql));
            return Add(SqlQuery.Simple<T>(sql));
        }

        private void AddExecutor(Action<IExecutionContext, int> executor)
        {
            var canAdd = Interlocked.CompareExchange(ref _beingRead, 0, 0) == 0;
            if (!canAdd)
                return;
            _executors.Enqueue(executor);
        }
    }
}