using CastIron.Sql.SqlServer;
using CastIron.Sql.Statements;

namespace CastIron.Sql
{
    /// <summary>
    /// Factory to create ISqlRunner instances for SQL Server and equivalent providers
    /// </summary>
    public static class RunnerFactory
    {
        /// <summary>
        /// Create the ISqlRunner for the given connection string
        /// </summary>
        /// <param name="connectionString">The connection string to use for SQL Server</param>
        /// <returns></returns>
        public static ISqlRunner Create(string connectionString)
        {
            return new SqlRunner(new SqlServerDbConnectionFactory(connectionString), new SqlServerStatementBuilder(), new SqlServerDataInteractionFactory(), new SqlServerConfiguration());
        }
    }
}