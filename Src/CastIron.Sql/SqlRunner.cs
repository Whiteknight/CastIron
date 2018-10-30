using System;
using System.Collections.Generic;
using CastIron.Sql.Execution;
using CastIron.Sql.Utility;

namespace CastIron.Sql
{
    /// <summary>
    /// Runner for Query and Command objects. 
    /// </summary>
    public class SqlRunner : ISqlRunner
    {
        // TODO: Interception mechanism so we can inspect and modify the sql code in passing
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IDataInteractionFactory _interactionFactory;

        public SqlRunner(IDbConnectionFactory connectionFactory, ISqlStatementBuilder statementBuilder, IDataInteractionFactory interactionFactory)
        {
            Assert.ArgumentNotNull(connectionFactory, nameof(connectionFactory));
            Assert.ArgumentNotNull(statementBuilder, nameof(statementBuilder));
            Assert.ArgumentNotNull(interactionFactory, nameof(interactionFactory));

            _connectionFactory = connectionFactory;
            _interactionFactory = interactionFactory;
            Statements = statementBuilder;

            Stringifier = new QueryObjectStringifier(_interactionFactory);
        }

        public QueryObjectStringifier Stringifier { get; }
        public ISqlStatementBuilder Statements { get; }

        public SqlBatch CreateBatch()
        {
            return new SqlBatch(_interactionFactory);
        }

        public void Execute(SqlBatch batch, Action<IContextBuilder> build = null)
        {
            Assert.ArgumentNotNull(batch, nameof(batch));

            Execute(batch.GetExecutors(), build);
        }

        public T Query<T>(ISqlQuerySimple<T> query, Action<IContextBuilder> build = null)
        {
            Assert.ArgumentNotNull(query, nameof(query));
            return Execute(c => new SqlQuerySimpleStrategy<T>(query).Execute(c, 0), build);
        }

        public T Query<T>(ISqlQuery<T> query, Action<IContextBuilder> build = null)
        {
            Assert.ArgumentNotNull(query, nameof(query));
            return Execute(c => new SqlQueryStrategy<T>(query, _interactionFactory).Execute(c, 0), build);
        }

        public T Query<T>(ISqlConnectionAccessor<T> accessor, Action<IContextBuilder> build = null)
        {
            Assert.ArgumentNotNull(accessor, nameof(accessor));
            return Execute(c => new SqlConnectionAccessorStrategy<T>(accessor).Execute(c, 0), build);
        }

        public void Execute(ISqlConnectionAccessor accessor, Action<IContextBuilder> build = null)
        {
            Assert.ArgumentNotNull(accessor, nameof(accessor));
            Execute(c => new SqlConnectionAccessorStrategy(accessor).Execute(c, 0), build);
        }

        public void Execute(ISqlCommandSimple commandObject, Action<IContextBuilder> build = null)
        {
            Assert.ArgumentNotNull(commandObject, nameof(commandObject));
            Execute(c => new SqlCommandStrategy(commandObject).Execute(c, 0), build);
        }

        public T Execute<T>(ISqlCommandSimple<T> commandObject, Action<IContextBuilder> build = null)
        {
            Assert.ArgumentNotNull(commandObject, nameof(commandObject));
            return Execute(c => new SqlCommandStrategy<T>(commandObject).Execute(c, 0), build);
        }

        public void Execute(ISqlCommand commandObject, Action<IContextBuilder> build = null)
        {
            Assert.ArgumentNotNull(commandObject, nameof(commandObject));
            Execute(c => new SqlCommandRawStrategy(commandObject, _interactionFactory).Execute(c, 0), build);
        }

        public T Execute<T>(ISqlCommand<T> commandObject, Action<IContextBuilder> build = null)
        {
            Assert.ArgumentNotNull(commandObject, nameof(commandObject));
            return Execute(c => new SqlCommandRawStrategy<T>(commandObject, _interactionFactory).Execute(c, 0), build);
        }

        public IDataResultsStream QueryStream(ISqlQuery query, Action<IContextBuilder> build = null)
        {
            Assert.ArgumentNotNull(query, nameof(query));
            var context = CreateExecutionContext(build);
            context.StartAction("Open connection");
            context.OpenConnection();
            return new SqlQueryStreamStrategy(query, _interactionFactory).Execute(context);
        }

        public IDataResultsStream QueryStream(ISqlQuerySimple query, Action<IContextBuilder> build = null)
        {
            Assert.ArgumentNotNull(query, nameof(query));
            var context = CreateExecutionContext(build);
            context.StartAction("Open connection");
            context.OpenConnection();
            return new SqlQuerySimpleStreamStrategy(query).Execute(context);
        }

        private ExecutionContext CreateExecutionContext(Action<IContextBuilder> build)
        {
            var connection = _connectionFactory.Create();
            var context = new ExecutionContext(connection);
            build?.Invoke(context);
            return context;
        }

        private void Execute(IReadOnlyList<Action<IExecutionContext, int>> executors, Action<IContextBuilder> build = null)
        {
            using (var context = CreateExecutionContext(build))
            {
                context.StartAction("Open connection");
                context.OpenConnection();
                for (int i = 0; i < executors.Count; i++)
                    executors[i](context, i);
                context.MarkComplete();
            }
        }

        private T Execute<T>(Func<IExecutionContext, T> executor, Action<IContextBuilder> build = null)
        {
            using (var context = CreateExecutionContext(build))
            {
                context.StartAction("Open connection");
                context.OpenConnection();
                var result = executor(context);
                context.MarkComplete();
                return result;
            }
        }

        private void Execute(Action<IExecutionContext> executor, Action<IContextBuilder> build = null)
        {
            using (var context = CreateExecutionContext(build))
            {
                context.StartAction("Open connection");
                context.OpenConnection();
                executor(context);
                context.MarkComplete();
            }
        }
    }
}