using System;
using CastIron.Sql;
using CastIron.Sql.Mapping;

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
        /// <param name="build"></param>
        /// <returns></returns>
        public static ISqlRunner Create(string connectionString, IMapCache mapCache = null, IMapCompilerSource compilerSource = null, Action<IContextBuilder> build = null)
        {
            var connectionFactory = new PostgresDbConnectionFactory(connectionString);
            mapCache = mapCache ?? new MapCache();
            compilerSource = compilerSource ?? new MapCompilerSource();
            return new SqlRunner(_core, connectionFactory, build, compilerSource, mapCache);
        }
    }
}
