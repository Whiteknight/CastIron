using CastIron.Sql;

namespace CastIron.Sqlite
{
    /// <summary>
    /// Sqlite configuration
    /// </summary>
    public class SqliteConfiguration : IProviderConfiguration
    {
        // Sqlite names the column by the expression which produces it, so there is no general-
        // purpose way to tell by name if the column was unnamed in the query
        public string UnnamedColumnName => null;
    }
}