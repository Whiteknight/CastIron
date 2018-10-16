using CastIron.Sql;
using CastIron.Sql.SqlServer;
using CastIron.Sql.Statements;

namespace CastIron.Sqlite
{
    public static class RunnerFactory
    {
        public static ISqlRunner Create(string connectionString)
        {
            return new SqlRunner(
                new SqliteDbConnectionFactory(connectionString), 
                new SqlServerStatementBuilder(), 
                // TODO: Do we need a different interaction wrapper for sqlite?
                new SqlServerDataInteractionFactory());
        }
    }
}