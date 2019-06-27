using CastIron.Sql;
using CastIron.Sql.Statements;

namespace CastIron.Postgres
{
    public static class RunnerFactory
    {
        public static ISqlRunner Create(string connectionString)
        {
            return new SqlRunner(
                new PostgresDbConnectionFactory(connectionString),
                // TODO: Do we need a custom one of these for Postgres?
                new SqlServerStatementBuilder(),
                new PostgresDataInteractionFactory(),
                new PostgresConfiguration()
                );
        }
    }
}
