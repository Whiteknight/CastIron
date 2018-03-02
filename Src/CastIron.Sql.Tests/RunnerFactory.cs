namespace CastIron.Sql.Tests
{
    public static class RunnerFactory
    {
        public static SqlQueryRunner Create()
        {
            //return new SqlQueryRunner("server=localhost;Integrated Security=SSPI;");
            return new SqlQueryRunner("server=localhost;user id=SA;password=passwordABC123;");
        }
    }
}
