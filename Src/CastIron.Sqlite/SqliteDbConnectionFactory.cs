using System.Data;
using System.Threading.Tasks;
using CastIron.Sql;
using Microsoft.Data.Sqlite;

namespace CastIron.Sqlite
{
    /// <summary>
    /// IDbConnection factory for Sqlite. Creates IDbConnection and async wrapper types
    /// </summary>
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

        public IDbConnectionAsync CreateForAsync()
        {
            return new DbConnectionAsync(new SqliteConnection(_connectionString));
        }

        public class DbConnectionAsync : IDbConnectionAsync
        {
            private readonly SqliteConnection _connection;

            public DbConnectionAsync(SqliteConnection connection)
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
            private readonly SqliteCommand _command;

            public DbCommandAsync(SqliteCommand command)
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
