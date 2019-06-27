using CastIron.Sql;

namespace CastIron.Postgres
{
    public class PostgresConfiguration : IProviderConfiguration
    {
        public string UnnamedColumnName => "?column?";
    }
}