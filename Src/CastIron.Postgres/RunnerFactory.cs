using System;
using CastIron.Sql;

namespace CastIron.Postgres
{
    /// <summary>
    /// Factory to create an ISqlRunner for a PostgreSQL database
    /// </summary>
    public static class RunnerFactory
    {
        private static readonly SqlRunnerCore _core = new SqlRunnerCore(new PostgresDataInteractionFactory(), new PostgresConfiguration(), new PostgresDbCommandStringifier());

        /// <summary>
        /// Create the runner for the given connection string
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="defaultBuilder"></param>
        /// <returns></returns>
        public static ISqlRunner Create(string connectionString, Action<IContextBuilder> defaultBuilder = null)
        {
            var connectionFactory = new PostgresDbConnectionFactory(connectionString);
            return new SqlRunner(_core, connectionFactory, defaultBuilder);
        }
    }
}
