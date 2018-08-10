using System;
using System.Collections.Generic;
using CastIron.Sql.Execution;
using CastIron.Sql.Statements;

namespace CastIron.Sql
{
    /// <summary>
    /// Runner for Query and Command objects. 
    /// </summary>
    public class SqlRunner : ISqlRunner
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public SqlRunner(string connectionString, IDbConnectionFactory connectionFactory = null)
        {
            _connectionFactory = connectionFactory ?? new SqlServerDbConnectionFactory(connectionString);
            Stringifier = new QueryObjectStringifier();
        }

        public QueryObjectStringifier Stringifier { get; }

        private ExecutionContext CreateExecutionContext(Action<IContextBuilder> build)
        {
            var connection = _connectionFactory.Create();
            var context =  new ExecutionContext(connection);
            build?.Invoke(context);
            return context;
        }

        public void Execute(IReadOnlyList<Action<IExecutionContext, int>> statements, Action<IContextBuilder> build = null)
        {
            using (var context = CreateExecutionContext(build))
            {
                context.StartAction("Open connection");
                context.OpenConnection();
                for (int i = 0; i < statements.Count; i++)
                    statements[i](context, i);
                context.MarkComplete();
            }
        }

        public T Execute<T>(Func<IExecutionContext, T> executor, Action<IContextBuilder> build = null)
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

        public void Execute(Action<IExecutionContext> executor, Action<IContextBuilder> build = null)
        {
            using (var context = CreateExecutionContext(build))
            {
                context.StartAction("Open connection");
                context.OpenConnection();
                executor(context);
                context.MarkComplete();
            }
        }

        public void Execute(SqlBatch batch, Action<IContextBuilder> build = null)
        {
            Execute(batch.GetExecutors(), build);
        }

        public void Execute(string sql, Action<IContextBuilder> build = null)
        {
            Execute(new SqlCommand(sql), build);
        }

        public T Query<T>(ISqlQuerySimple<T> query, Action<IContextBuilder> build = null)
        {
            return Execute(c => new SqlQueryStrategy<T>(query).Execute(c, 0), build);
        }

        public T Query<T>(ISqlQuery<T> query, Action<IContextBuilder> build = null)
        {
            return Execute(c => new SqlQueryRawCommandStrategy<T>(query).Execute(c, 0), build);
        }

        public T Query<T>(ISqlConnectionAccessor<T> query, Action<IContextBuilder> build = null)
        {
            return Execute(c => new SqlQueryRawConnectionStrategy<T>(query).Execute(c, 0), build);
        }

        public IReadOnlyList<T> Query<T>(string sql, Action<IContextBuilder> build = null)
        {
            return Query(new SqlQuery<T>(sql), build);
        }

        public void Execute(ISqlCommandSimple commandObject, Action<IContextBuilder> build = null)
        {
            Execute(c => new SqlCommandStrategy(commandObject).Execute(c, 0), build);
        }

        public T Execute<T>(ISqlCommandSimple<T> commandObject, Action<IContextBuilder> build = null)
        {
            return Execute(c => new SqlCommandStrategy<T>(commandObject).Execute(c, 0), build);
        }

        public void Execute(ISqlCommand commandObject, Action<IContextBuilder> build = null)
        {
            Execute(c => new SqlCommandRawStrategy(commandObject).Execute(c, 0), build);
        }

        public T Execute<T>(ISqlCommand<T> commandObject, Action<IContextBuilder> build = null)
        {
            return Execute(c => new SqlCommandRawStrategy<T>(commandObject).Execute(c, 0), build);
        }

        // TODO: Method variants where we can get a wrapped object which contains the (still open) connection, so we can stream data and then dispose the whole thing
        // TODO: This will probably be something like SqlResultSet, but with the connection and a .Dispose() method and logic to auto-dispose on stream end.
    }
}