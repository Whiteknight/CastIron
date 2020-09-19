using System;
using CastIron.Sql;

namespace CastIron.SqlServer
{
    /// <summary>
    /// Factory to create ISqlRunner instances for SQL Server and equivalent providers
    /// </summary>
    public static class RunnerFactory
    {
        private static readonly SqlRunnerCore _core = new SqlRunnerCore(new SqlServerDataInteractionFactory(), new SqlServerConfiguration(), new SqlServerDbCommandStringifier());

        /// <summary>
        /// Create the ISqlRunner for the given connection string
        /// </summary>
        /// <param name="connectionString">The connection string to use for SQL Server</param>
        /// <param name="defaultBuilder">Set default options on all execution contexts</param>
        /// <returns></returns>
        public static ISqlRunner Create(string connectionString, Action<IContextBuilder> defaultBuilder = null)
        {
            // TODO: We can cache the connection factory by connection string.
            var connectionFactory = new SqlServerDbConnectionFactory(connectionString);
            return new SqlRunner(_core, connectionFactory, defaultBuilder);
        }
    }
}