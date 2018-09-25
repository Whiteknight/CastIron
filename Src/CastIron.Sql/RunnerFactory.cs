namespace CastIron.Sql
{
    public static class RunnerFactory
    {
        public static ISqlRunner Create(string connectionString)
        {
            return new SqlRunner(connectionString, new SqlServerDbConnectionFactory(connectionString));
        }
    }
}