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
            PreferReadOnly = true;
        }

        public bool PreferReadOnly { get; private set; }

        public IReadOnlyList<Action<IExecutionContext, int>> GetExecutors()
        {
            var beingRead = Interlocked.CompareExchange(ref _beingRead, 1, 0) == 0;
            if (beingRead)
                return _executors.ToArray();
            return new Action<IExecutionContext, int>[0];
        }

        private void AddExecutor(Action<IExecutionContext, int> executor, bool preferRo)
        {
            var canAdd = Interlocked.CompareExchange(ref _beingRead, 0, 0) == 0;
            if (!canAdd)
                return;
            _executors.Enqueue(executor);
            PreferReadOnly &= preferRo;
        }

        public ISqlResultPromise<T> Add<T>(ISqlQuery<T> query)
        {
            var result = new SqlResultPromise<T>();
            AddExecutor((c, i) => result.SetValue(new SqlQueryStrategy<T>(query).Execute(c, i)), query is ISqlReadOnly);
            return result;
        }

        public ISqlResultPromise<T> Add<T>(ISqlQueryRawCommand<T> query)
        {
            var result = new SqlResultPromise<T>();
            AddExecutor((c, i) => result.SetValue(new SqlQueryRawCommandStrategy<T>(query).Execute(c, i)), query is ISqlReadOnly);
            return result;
        }

        public ISqlResultPromise<T> Add<T>(ISqlQueryRawConnection<T> query)
        {
            var result = new SqlResultPromise<T>();
            AddExecutor((c, i) => result.SetValue(new SqlQueryRawConnectionStrategy<T>(query).Execute(c, i)), query is ISqlReadOnly);
            return result;
        }

        public ISqlResultPromise Add(ISqlCommand command)
        {
            var result = new SqlResultPromise();
            AddExecutor((c, i) =>
            {
                new SqlCommandStrategy(command).Execute(c, i);
                result.IsComplete = true;
            }, command is ISqlReadOnly);
            return result;
        }

        public ISqlResultPromise Add(ISqlCommandRawCommand command)
        {
            var result = new SqlResultPromise();
            AddExecutor((c, i) =>
            {
                new SqlCommandRawStrategy(command).Execute(c, i);
                result.IsComplete = true;
            }, command is ISqlReadOnly);
            return result;
        }

        public ISqlResultPromise<T> Add<T>(ISqlCommandRawCommand<T> command)
        {
            var result = new SqlResultPromise<T>();
            AddExecutor((c, i) => result.SetValue(new SqlCommandRawStrategy<T>(command).Execute(c, i)), command is ISqlReadOnly);
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