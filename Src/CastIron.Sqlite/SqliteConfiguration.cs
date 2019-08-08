using CastIron.Sql;

namespace CastIron.Sqlite
{
    /// <summary>
    /// Sqlite configuration
    /// </summary>
    public class SqliteConfiguration : IProviderConfiguration
    {
        // TODO: Sqlite names the column by the expression which produces it, so this won't work
        // What we want instead of a Func<string, bool> predicate to tell if a name looks like unnamed
        public string UnnamedColumnName => string.Empty;
    }
}