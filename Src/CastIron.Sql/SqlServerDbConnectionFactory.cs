using System.Data;
using System.Data.SqlClient;

namespace CastIron.Sql
{
    /// <summary>
    /// Default IDbConnectionFactory implementation for Microsoft SQL Server and compatible databases
    /// </summary>
    public class SqlServerDbConnectionFactory : IDbConnectionFactory
    {
        public IDbConnection Create(string connectionString)
        {
            return new SqlConnection(connectionString);
        }
    }
}