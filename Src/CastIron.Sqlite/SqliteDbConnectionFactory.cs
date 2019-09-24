using System.Data;
using System.Threading;
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

        public IDbConnectionAsync Create()
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

            public Task OpenAsync(CancellationToken cancellationToken)
            {
                return _connection.OpenAsync(cancellationToken);
            }

            public IDbCommandAsync CreateCommand()
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

            public IDataReaderAsync ExecuteReader()
            {
                return new DataReaderAsync(_command.ExecuteReader());
            }

            public async Task<IDataReaderAsync> ExecuteReaderAsync(CancellationToken cancellationToken)
            {
                var reader = await _command.ExecuteReaderAsync(cancellationToken);
                return new DataReaderAsync(reader);
            }

            public Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
            {
                return _command.ExecuteNonQueryAsync(cancellationToken);
            }

            public void Dispose()
            {
                _command?.Dispose();
            }
        }

        public class DataReaderAsync : IDataReaderAsync
        {
            private readonly SqliteDataReader _reader;

            public DataReaderAsync(SqliteDataReader reader)
            {
                _reader = reader;
            }

            public void Dispose()
            {
                _reader.Dispose();
            }

            public IDataReader Reader => _reader;

            public Task<bool> NextResultAsync(CancellationToken cancellationToken)
            {
                return _reader.NextResultAsync(cancellationToken);
            }

            public Task<bool> ReadAsync(CancellationToken cancellationToken)
            {
                return _reader.ReadAsync(cancellationToken);
            }
        }
    }
}
