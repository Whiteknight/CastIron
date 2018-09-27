using CastIron.Sql;
using CastIron.Sql.Statements;

namespace CastIron.Sqlite
{
    public static class RunnerFactory
    {
        public static ISqlRunner Create(string connectionString)
        {
            return new SqlRunner(new SqliteDbConnectionFactory(connectionString), new SqlStatementBuilder());
        }
    }
}