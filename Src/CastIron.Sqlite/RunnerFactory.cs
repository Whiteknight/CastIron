using System;
using CastIron.Sql;
using CastIron.Sql.Mapping;

namespace CastIron.Sqlite
{
    /// <summary>
    /// Factory to create ISqlRunner instances for SQLite
    /// </summary>
    public static class RunnerFactory
    {
        private static readonly SqlRunnerCore _core = new SqlRunnerCore(new SqliteDataInteractionFactory(), new SqliteConfiguration(), new SqliteDbCommandStringifier());

        public static ISqlRunner Create(string connectionString, IMapCache mapCache = null, IMapCompilerSource compilerSource = null, Action<IContextBuilder> build = null)
        {
            var connectionFactory = new SqliteDbConnectionFactory(connectionString);
            mapCache = mapCache ?? new MapCache();
            compilerSource = compilerSource ?? new MapCompilerSource();
            return new SqlRunner(_core, connectionFactory, build, compilerSource, mapCache);
        }
    }
}
