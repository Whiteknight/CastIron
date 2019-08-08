using CastIron.Sql;

namespace CastIron.Postgres
{
    /// <summary>
    /// Configuration for Postgres
    /// </summary>
    public class PostgresConfiguration : IProviderConfiguration
    {
        /// <summary>
        /// Columns without an explicit name in Postgres are named '?column?' for some reason
        /// </summary>
        public string UnnamedColumnName => "?column?";
    }
}