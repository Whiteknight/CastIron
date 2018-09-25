using CastIron.Sql;

namespace CastIron.Sqlite
{
    public static class RunnerFactory
    {
        public static ISqlRunner Create(string connectionString)
        {
            return new SqlRunner(connectionString, new SqliteDbConnectionFactory(connectionString));
        }
    }
}