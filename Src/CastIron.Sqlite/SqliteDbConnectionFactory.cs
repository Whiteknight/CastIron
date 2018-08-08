using System.Data;
using CastIron.Sql;
using Microsoft.Data.Sqlite;

namespace CastIron.Sqlite
{
    public class SqliteDbConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public SqliteDbConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection Create(string connectionString)
        {
            return new SqliteConnection(connectionString);
        }

        public IDbConnection Create()
        {
            return Create(_connectionString);
        }
    }
}
