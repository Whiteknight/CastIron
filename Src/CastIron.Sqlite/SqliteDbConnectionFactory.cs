using CastIron.Sql;
using CastIron.Sql.Utility;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

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
            Argument.NotNullOrEmpty(connectionString, nameof(connectionString));
            _connectionString = connectionString;
        }

        public IDbConnectionAsync Create() => new DbConnectionAsync(new SqliteConnection(_connectionString));

        public sealed class DbConnectionAsync : IDbConnectionAsync
        {
            private readonly SqliteConnection _connection;

            public DbConnectionAsync(SqliteConnection connection)
            {
                _connection = connection;
            }

            public IDbConnection Connection => _connection;

            public Task OpenAsync(CancellationToken cancellationToken) => _connection.OpenAsync(cancellationToken);

            public IDbCommandAsync CreateCommand() => new DbCommandAsync(_connection.CreateCommand());

            public void Dispose() => _connection?.Dispose();
        }

        public sealed class DbCommandAsync : IDbCommandAsync
        {
            private readonly SqliteCommand _command;

            public DbCommandAsync(SqliteCommand command)
            {
                _command = command;
            }

            public IDbCommand Command => _command;

            public IDataReaderAsync ExecuteReader() => new DataReaderAsync(_command.ExecuteReader());

            public async Task<IDataReaderAsync> ExecuteReaderAsync(CancellationToken cancellationToken)
            {
                var reader = await _command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                return new DataReaderAsync(reader);
            }

            public Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) => _command.ExecuteNonQueryAsync(cancellationToken);

            public void Dispose() => _command?.Dispose();
        }

        public sealed class DataReaderAsync : IDataReaderAsync
        {
            private readonly SqliteDataReader _reader;

            public DataReaderAsync(SqliteDataReader reader)
            {
                _reader = reader;
            }

            public void Dispose() => _reader.Dispose();

            public IDataReader Reader => _reader;

            public Task<bool> NextResultAsync(CancellationToken cancellationToken) => _reader.NextResultAsync(cancellationToken);

            public Task<bool> ReadAsync(CancellationToken cancellationToken) => _reader.ReadAsync(cancellationToken);
        }
    }
}
