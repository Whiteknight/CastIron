using System.Data;
using System.Data.SqlClient;

namespace CastIron.Sql.SqlServer
{
    /// <summary>
    /// Default IDbConnectionFactory implementation for Microsoft SQL Server and compatible databases
    /// </summary>
    public class SqlServerDbConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public SqlServerDbConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection Create()
        {
            return new SqlConnection(_connectionString);
        }
    }
}