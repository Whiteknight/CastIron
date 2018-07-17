using System.Collections.Generic;
using System.Data;
using CastIron.Sql.Execution;

namespace CastIron.Sql
{
    public class SqlQueryRunner
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public SqlQueryRunner(string connectionString, IDbConnectionFactory connectionFactory = null)
        {
            _connectionFactory = connectionFactory ?? new SqlServerDbConnectionFactory(connectionString);
        }

        public object[] Execute(IReadOnlyList<IExecutionStrategy> statements, bool useTransaction = false)
        {
            var results = new object[statements.Count];
            using (var connection = _connectionFactory.Create())
            {
                IDbTransaction transaction = null;
                if (useTransaction)
                    transaction = connection.BeginTransaction();
                try
                {
                    
                    connection.Open();
                    for (int i = 0; i < statements.Count; i++)
                        results[i] = statements[i].Execute(connection, transaction, i);
                }
                finally
                {
                    transaction?.Commit();
                    transaction?.Dispose();
                }
            }

            return results;
        }

        public T Query<T>(ISqlQuery<T> query)
        {
            return (T) Execute(new [] {new SqlQueryStratgy<T>(query) })[0];
        }

        public T Query<T>(ISqlQueryRawCommand<T> query)
        {
            return (T)Execute(new[] { new SqlQueryRawCommandStrategy<T>(query) })[0];
        }

        public T Query<T>(ISqlQueryRawConnection<T> query)
        {
            return (T)Execute(new[] { new SqlQueryRawConnectionStrategy<T>(query) })[0];
        }

        public void Execute(ISqlCommand commandObject)
        {
            Execute(new[] { new SqlCommandStrategy(commandObject) });
        }

        public T Execute<T>(ISqlCommand<T> commandObject)
        {
            return (T) Execute(new[] { new SqlCommandStrategy<T>(commandObject) })[0];
        }

        public void Execute(ISqlCommandRaw commandObject)
        {
            Execute(new[] { new SqlCommandRawStrategy(commandObject) });
        }

        public T Execute<T>(ISqlCommandRaw<T> commandObject)
        {
            return (T) Execute(new[] { new SqlCommandRawStrategy<T>(commandObject) })[0];
        }
    }
}