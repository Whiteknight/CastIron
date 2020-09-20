using System;
using CastIron.Sql;

namespace CastIron.Sqlite
{
    /// <summary>
    /// Factory to create ISqlRunner instances for SQLite
    /// </summary>
    public static class RunnerFactory
    {
        private static readonly SqlRunnerCore _core = new SqlRunnerCore(new SqliteDataInteractionFactory(), new SqliteConfiguration(), new SqliteDbCommandStringifier());

        public static ISqlRunner Create(string connectionString, Action<IContextBuilder> defaultBuilder = null)
        {
            var connectionFactory = new SqliteDbConnectionFactory(connectionString);
            return new SqlRunner(_core, connectionFactory, defaultBuilder, new Sql.Mapping.MapCompilerSource(true));
        }
    }
}