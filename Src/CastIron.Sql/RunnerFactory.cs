using CastIron.Sql.Statements;

namespace CastIron.Sql
{
    public static class RunnerFactory
    {
        public static ISqlRunner Create(string connectionString)
        {
            return new SqlRunner(new SqlServerDbConnectionFactory(connectionString), new SqlStatementBuilder());
        }
    }
}