using System.Data;
using System.Threading.Tasks;
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

        public IDbConnectionAsync CreateForAsync()
        {
            return new DbConnectionAsync(new NpgsqlConnection(_connectionString));
        }

        public class DbConnectionAsync : IDbConnectionAsync
        {
            private readonly NpgsqlConnection _connection;

            public DbConnectionAsync(NpgsqlConnection connection)
            {
                _connection = connection;
            }

            public IDbConnection Connection => _connection;

            public Task OpenAsync()
            {
                return _connection.OpenAsync();
            }

            public IDbCommandAsync CreateAsyncCommand()
            {
                return new DbCommandAsync(_connection.CreateCommand());
            }

            public void Dispose()
            {
                _connection?.Dispose();
            }
        }

        public class DbCommandAsync : IDbCommandAsync
        {
            private readonly NpgsqlCommand _command;

            public DbCommandAsync(NpgsqlCommand command)
            {
                _command = command;
            }

            public IDbCommand Command => _command;

            public async Task<IDataReader> ExecuteReaderAsync()
            {
                return await _command.ExecuteReaderAsync();
            }

            public Task<int> ExecuteNonQueryAsync()
            {
                return _command.ExecuteNonQueryAsync();
            }

            public void Dispose()
            {
                _command?.Dispose();
            }

        }
    }
}
