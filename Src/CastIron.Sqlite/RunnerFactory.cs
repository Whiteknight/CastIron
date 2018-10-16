using CastIron.Sql;
using CastIron.Sql.Statements;

namespace CastIron.Sqlite
{
    public static class RunnerFactory
    {
        public static ISqlRunner Create(string connectionString)
        {
            return new SqlRunner(
                new SqliteDbConnectionFactory(connectionString), 
                // TODO: Do we need a custom one of these for SQLite?
                new SqlServerStatementBuilder(), 
                new SqliteDataInteractionFactory());
        }
    }
}