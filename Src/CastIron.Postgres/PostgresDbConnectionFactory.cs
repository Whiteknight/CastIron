using System.Data;
using CastIron.Sql;
using Npgsql;

namespace CastIron.Postgres
{
    public class PostgresDbConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public PostgresDbConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection Create(string connectionString)
        {
            return new NpgsqlConnection(connectionString);
        }

        public IDbConnection Create()
        {
            return Create(_connectionString);
        }
    }
}
