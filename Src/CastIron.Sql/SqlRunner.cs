using System;
using System.Collections.Generic;
using CastIron.Sql.Execution;
using CastIron.Sql.Statements;

namespace CastIron.Sql
{
    /// <summary>
    /// Runner for Query and Command objects. 
    /// </summary>
    public class SqlRunner
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ConnectionStrings _connectionStrings;

        public SqlRunner(string connectionString, string roConnectionString = null, IDbConnectionFactory connectionFactory = null)
            :this(new ConnectionStrings(connectionString, roConnectionString), connectionFactory)
        {
        }

        public SqlRunner(ConnectionStrings connectionStrings, IDbConnectionFactory connectionFactory = null)
        {
            Assert.ArgumentNotNull(connectionStrings, nameof(connectionStrings));
            _connectionStrings = connectionStrings;
            _connectionFactory = connectionFactory ?? new SqlServerDbConnectionFactory();
            Stringifier = new QueryObjectStringifier();
        }

        public QueryObjectStringifier Stringifier { get; }

        private ExecutionContext CreateExecutionContext(Action<IContextBuilder> build, bool preferRo)
        {
            var context = new ExecutionContext();
            build?.Invoke(context);

            var connectionStringName = context.ConnectionStringName;
            if (string.IsNullOrEmpty(connectionStringName) && preferRo)
                connectionStringName = ConnectionStrings.ReadOnly;

            var connectionString = _connectionStrings.Get(connectionStringName);
            var connection = _connectionFactory.Create(connectionString);
            context.SetConnection(connection);
            
            return context;
        }

        private void Execute(IReadOnlyList<Action<IExecutionContext, int>> statements, Action<IContextBuilder> build, bool preferRo)
        {
            using (var context = CreateExecutionContext(build, preferRo))
            {
                context.StartAction("Open connection");
                context.OpenConnection();
                for (int i = 0; i < statements.Count; i++)
                    statements[i](context, i);
                context.MarkComplete();
            }
        }

        private T Execute<T>(Func<IExecutionContext, T> executor, Action<IContextBuilder> build, bool preferRo)
        {
            using (var context = CreateExecutionContext(build, preferRo))
            {
                context.StartAction("Open connection");
                context.OpenConnection();
                var result = executor(context);
                context.MarkComplete();
                return result;
            }
        }

        private void Execute(Action<IExecutionContext> executor, Action<IContextBuilder> build, bool preferRo)
        {
            using (var context = CreateExecutionContext(build, preferRo))
            {
                context.StartAction("Open connection");
                context.OpenConnection();
                executor(context);
                context.MarkComplete();
            }
        }

        public void Execute(SqlBatch batch, Action<IContextBuilder> build = null)
        {
            Execute(batch.GetExecutors(), build, batch.PreferReadOnly);
        }

        public void Execute(string sql, Action<IContextBuilder> build = null)
        {
            Execute(new SqlCommand(sql), build);
        }

        public T Query<T>(ISqlQuery<T> query, Action<IContextBuilder> build = null)
        {
            return Execute(c => new SqlQueryStrategy<T>(query).Execute(c, 0), build, query is ISqlReadOnly);
        }

        public T Query<T>(ISqlQueryRawCommand<T> query, Action<IContextBuilder> build = null)
        {
            return Execute(c => new SqlQueryRawCommandStrategy<T>(query).Execute(c, 0), build, query is ISqlReadOnly);
        }

        public T Query<T>(ISqlQueryRawConnection<T> query, Action<IContextBuilder> build = null)
        {
            return Execute(c => new SqlQueryRawConnectionStrategy<T>(query).Execute(c, 0), build, query is ISqlReadOnly);
        }

        public IReadOnlyList<T> Query<T>(string sql, Action<IContextBuilder> build = null)
        {
            return Query(new SqlQuery<T>(sql), build);
        }

        public void Execute(ISqlCommand command, Action<IContextBuilder> build = null)
        {
            Execute(c => new SqlCommandStrategy(command).Execute(c, 0), build, command is ISqlReadOnly);
        }

        public T Execute<T>(ISqlCommand<T> command, Action<IContextBuilder> build = null)
        {
            return Execute(c => new SqlCommandStrategy<T>(command).Execute(c, 0), build, command is ISqlReadOnly);
        }

        public void Execute(ISqlCommandRawCommand command, Action<IContextBuilder> build = null)
        {
            Execute(c => new SqlCommandRawStrategy(command).Execute(c, 0), build, command is ISqlReadOnly);
        }

        public T Execute<T>(ISqlCommandRawCommand<T> command, Action<IContextBuilder> build = null)
        {
            return Execute(c => new SqlCommandRawStrategy<T>(command).Execute(c, 0), build, command is ISqlReadOnly);
        }

        // TODO: Method variants where we can get a wrapped object which contains the (still open) connection, so we can stream data and then dispose the whole thing
        // TODO: This will probably be something like SqlResultSet, but with the connection and a .Dispose() method and logic to auto-dispose on stream end.
    }
}