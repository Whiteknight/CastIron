using System;
using CastIron.Sql;
using CastIron.Sql.Mapping;

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
        /// <param name="build">Set default options on all execution contexts</param>
        /// <returns></returns>
        public static ISqlRunner Create(string connectionString, IMapCache mapCache = null, IMapCompilerSource compilerSource = null, Action<IContextBuilder> build = null)
        {
            var connectionFactory = new SqlServerDbConnectionFactory(connectionString);
            mapCache = mapCache ?? new MapCache();
            compilerSource = compilerSource ?? new MapCompilerSource();
            return new SqlRunner(_core, connectionFactory, build, compilerSource, mapCache);
        }
    }
}