﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using CastIron.Sql.Execution;
using CastIron.Sql.Statements;
using CastIron.Sql.Utility;

namespace CastIron.Sql
{
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
            Argument.NotNull(query, nameof(query));
            Argument.NotNull(reader, nameof(reader));
            var promise = new SqlResultPromise<T>();
            AddExecutor((c, i) =>
            {
                var result = new SqlQuerySimpleStrategy().Execute(query, reader, c, i);
                promise.SetValue(result);
            });
            return promise;
        }

        public ISqlResultPromise<T> Add<T>(ISqlQuerySimple<T> query) => Add(query, query);

        public ISqlResultPromise<T> Add<T>(ISqlQuery query, IResultMaterializer<T> reader)
        {
            Argument.NotNull(query, nameof(query));
            Argument.NotNull(reader, nameof(reader));
            var promise = new SqlResultPromise<T>();
            AddExecutor((c, i) =>
            {
                var result = new SqlQueryStrategy(_interactionFactory).Execute(query, reader, c, i);
                promise.SetValue(result);
            });
            return promise;
        }

        public ISqlResultPromise<T> Add<T>(ISqlQuery<T> query) => Add(query, query);

        public ISqlResultPromise<T> Add<T>(Func<IDataInteraction, bool> setup, IResultMaterializer<T> materializer)
            => Add(SqlQuery.FromDelegate(setup), materializer);

        public ISqlResultPromise<T> Add<T>(Func<IDataInteraction, bool> setup, Func<IDataResults, T> materialize)
            => Add(SqlQuery.FromDelegate(setup), Materializer.FromDelegate(materialize));

        public ISqlResultPromise Add(ISqlCommandSimple command)
        {
            Argument.NotNull(command, nameof(command));
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
            Argument.NotNull(command, nameof(command));
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
            Argument.NotNull(command, nameof(command));
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
            Argument.NotNullOrEmpty(sql, nameof(sql));
            return Add(new SqlCommand(sql));
        }

        public ISqlResultPromise<IReadOnlyList<T>> Add<T>(string sql)
        {
            Argument.NotNullOrEmpty(sql, nameof(sql));
            return Add(SqlQuery.FromString<T>(sql));
        }

        public ISqlResultPromise Add(Func<IDataInteraction, bool> setup)
            => Add(new SqlCommandFromDelegate(setup));

        private void AddExecutor(Action<IExecutionContext, int> executor)
        {
            var canAdd = Interlocked.CompareExchange(ref _beingRead, 0, 0) == 0;
            if (!canAdd)
                return;
            _executors.Enqueue(executor);
        }
    }
}
