using System.Data;
using CastIron.Sql;
using MySql.Data.MySqlClient;

namespace CastIron.MySql
{
    public class MySqlDbConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public MySqlDbConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection Create(string connectionString)
        {
            return new MySqlConnection(connectionString);
        }

        public IDbConnection Create()
        {
            return Create(_connectionString);
        }
    }
}
