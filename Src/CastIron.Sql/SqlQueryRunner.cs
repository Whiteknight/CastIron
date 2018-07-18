using System;
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

        public void Execute(ISqlCommand commandObject)
        {
            Execute(c => new SqlCommandStrategy(commandObject).Execute(c, 0));
        }

        public T Execute<T>(ISqlCommand<T> commandObject)
        {
            return Execute(c => new SqlCommandStrategy<T>(commandObject).Execute(c, 0));
        }

        public void Execute(ISqlCommandRaw commandObject)
        {
            Execute(c => new SqlCommandRawStrategy(commandObject).Execute(c, 0));
        }

        public T Execute<T>(ISqlCommandRaw<T> commandObject)
        {
            return Execute(c => new SqlCommandRawStrategy<T>(commandObject).Execute(c, 0));
        }
    }
}