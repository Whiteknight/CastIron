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

        public ISqlResultPromise Add(string sql)
        {
            return Add(new SqlCommand(sql));
        }

        public ISqlResultPromise<IReadOnlyList<T>> Add<T>(string sql)
        {
            return Add(new SqlQuery<T>(sql));
        }

        public ParameterizedInstance<T, TParameters> Add<T, TParameters>(ISqlQueryParameterized<T, TParameters> query)
        {
            var instances = new ParameterizedInstance<T, TParameters>();
            AddExecutor((c, i) => new SqlQueryParameterizedStrategy<T, TParameters>(query, instances).Execute(c, i));
            return instances;
        }
    }

    public class ParameterizedInstance<T, TParameters>
    {
        private readonly List<Tuple<TParameters, SqlResultPromise<T>>> _instances;

        public ParameterizedInstance()
        {
            _instances = new List<Tuple<TParameters, SqlResultPromise<T>>>();
        }

        public ISqlResultPromise<T> Use(TParameters parameters)
        {
            var promise = new SqlResultPromise<T>();
            _instances.Add(new Tuple<TParameters, SqlResultPromise<T>>(parameters, promise));
            return promise;
        }

        public IReadOnlyList<Tuple<TParameters, SqlResultPromise<T>>> GetInstances()
        {
            return _instances;
        }
    }

    public class SqlQueryParameterizedStrategy<T, TParameters>
    {
        private readonly ISqlQueryParameterized<T, TParameters> _query;
        private readonly ParameterizedInstance<T, TParameters> _instances;

        public SqlQueryParameterizedStrategy(ISqlQueryParameterized<T, TParameters> query, ParameterizedInstance<T, TParameters> instances)
        {
            _query = query;
            _instances = instances;
        }

        public void Execute(IExecutionContext context, int index)
        {
            using (var command = context.CreateCommand())
            {
                _query.SetupCommand(command);
                // TODO: We only need to .Prepare if there is more than one parameterset
                command.Prepare();

                foreach (var param in _instances.GetInstances())
                {
                    // TODO: Some validation that we are only setting values on existing parameters, not creating new ones
                    _query.SetValues(command, param.Item1);
                    using (var reader = command.ExecuteReader())
                    {
                        var resultSet = new SqlResultSet(command, context, reader);
                        var result = _query.Read(resultSet);
                        param.Item2.SetValue(result);
                    }
                }
            }
        }
    }
}