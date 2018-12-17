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
        

        public SqlRunner(IDbConnectionFactory connectionFactory, ISqlStatementBuilder statementBuilder, IDataInteractionFactory interactionFactory)
        {
            Assert.ArgumentNotNull(connectionFactory, nameof(connectionFactory));
            Assert.ArgumentNotNull(statementBuilder, nameof(statementBuilder));
            Assert.ArgumentNotNull(interactionFactory, nameof(interactionFactory));

            _connectionFactory = connectionFactory;
            InteractionFactory = interactionFactory;
            Statements = statementBuilder;

            Stringifier = new QueryObjectStringifier(InteractionFactory);
        }

        public IDataInteractionFactory InteractionFactory { get; }
        public QueryObjectStringifier Stringifier { get; }
        public ISqlStatementBuilder Statements { get; }

        public SqlBatch CreateBatch()
        {
            return new SqlBatch(InteractionFactory);
        }

        public ExecutionContext CreateExecutionContext(Action<IContextBuilder> build)
        {
            var connection = _connectionFactory.Create();
            var context = new ExecutionContext(connection);
            build?.Invoke(context);
            return context;
        }

        public void Execute(IReadOnlyList<Action<IExecutionContext, int>> executors, Action<IContextBuilder> build)
        {
            using (var context = CreateExecutionContext(build))
            {
                context.StartAction("Open connection");
                context.OpenConnection();
                for (var i = 0; i < executors.Count; i++)
                    executors[i](context, i);
                context.MarkComplete();
            }
        }

        public T Execute<T>(Func<IExecutionContext, T> executor, Action<IContextBuilder> build)
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

        public void Execute(Action<IExecutionContext> executor, Action<IContextBuilder> build)
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