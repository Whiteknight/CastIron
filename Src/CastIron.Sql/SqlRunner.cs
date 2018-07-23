using System;
using System.Collections.Generic;
using System.Data;
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

        public SqlRunner(string connectionString, IDbConnectionFactory connectionFactory = null)
        {
            _connectionFactory = connectionFactory ?? new SqlServerDbConnectionFactory(connectionString);
        }

        // TODO: Instead of "bool useTransaction" use an enum for the isolation level requested

        private ExecutionContext CreateExecutionContext(bool useTransaction)
        {
            var connection = _connectionFactory.Create();
            IDbTransaction transaction = useTransaction ? connection.BeginTransaction() : null;
            return new ExecutionContext(connection, transaction);
        }

        public void Execute(IReadOnlyList<Action<IExecutionContext, int>> statements, bool useTransaction = false)
        {
            using (var context = CreateExecutionContext(useTransaction))
            {
                context.Connection.Open();
                for (int i = 0; i < statements.Count; i++)
                    statements[i](context, i);
            }
        }

        public T Execute<T>(Func<IExecutionContext, T> executor, bool useTransaction = false)
        {
            using (var context = CreateExecutionContext(useTransaction))
            {
                context.Connection.Open();
                return executor(context);
            }
        }

        public void Execute(Action<IExecutionContext> executor, bool useTransaction = false)
        {
            using (var context = CreateExecutionContext(useTransaction))
            {
                context.Connection.Open();
                executor(context);
            }
        }

        public void Execute(SqlStatementBatch batch, bool useTransaction = false)
        {
            Execute(batch.GetExecutors(), useTransaction);
        }

        public void Execute(string sql)
        {
            Execute(new SqlCommand(sql));
        }

        public T Query<T>(ISqlQuery<T> query)
        {
            return Execute(c => new SqlQueryStrategy<T>(query).Execute(c, 0));
        }

        public T Query<T>(ISqlQueryRawCommand<T> query)
        {
            return Execute(c => new SqlQueryRawCommandStrategy<T>(query).Execute(c, 0));
        }

        public T Query<T>(ISqlQueryRawConnection<T> query)
        {
            return Execute(c => new SqlQueryRawConnectionStrategy<T>(query).Execute(c, 0));
        }

        public IReadOnlyList<T> Query<T>(string sql)
        {
            return Query(new SqlQuery<T>(sql));
        }

        public void Execute(ISqlCommand commandObject)
        {
            Execute(c => new SqlCommandStrategy(commandObject).Execute(c, 0));
        }

        public T Execute<T>(ISqlCommand<T> commandObject)
        {
            return Execute(c => new SqlCommandStrategy<T>(commandObject).Execute(c, 0));
        }

        public void Execute(ISqlCommandRawCommand commandObject)
        {
            Execute(c => new SqlCommandRawStrategy(commandObject).Execute(c, 0));
        }

        public T Execute<T>(ISqlCommandRawCommand<T> commandObject)
        {
            return Execute(c => new SqlCommandRawStrategy<T>(commandObject).Execute(c, 0));
        }

        // TODO: Method variants where we can get a wrapped object which contains the (still open) connection, so we can stream data and then dispose the whole thing
        // TODO: This will probably be something like SqlResultSet, but with the connection and a .Dispose() method and logic to auto-dispose on stream end.
    }
}