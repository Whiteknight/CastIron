using CastIron.Sql;
using CastIron.Sql.Utility;
using Npgsql;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace CastIron.Postgres
{
    /// <summary>
    /// Factory to create Postgres IDbConnection and async wrappers
    /// </summary>
    public class PostgresDbConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public PostgresDbConnectionFactory(string connectionString)
        {
            Argument.NotNullOrEmpty(connectionString, nameof(connectionString));
            _connectionString = connectionString;
        }

        public IDbConnectionAsync Create()
        {
            return new DbConnectionAsync(new NpgsqlConnection(_connectionString));
        }

        public sealed class DbConnectionAsync : IDbConnectionAsync
        {
            private readonly NpgsqlConnection _connection;

            public DbConnectionAsync(NpgsqlConnection connection)
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

        public sealed class DbCommandAsync : IDbCommandAsync
        {
            private readonly NpgsqlCommand _command;

            public DbCommandAsync(NpgsqlCommand command)
            {
                _command = command;
            }

            public IDbCommand Command => _command;

            public IDataReaderAsync ExecuteReader()
            {
                var reader = _command.ExecuteReader();
                return new DataReaderAsync(reader);
            }

            public async Task<IDataReaderAsync> ExecuteReaderAsync(CancellationToken cancellationToken)
            {
                var reader = await _command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                return new DataReaderAsync(reader as NpgsqlDataReader);
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

        public sealed class DataReaderAsync : IDataReaderAsync
        {
            private readonly NpgsqlDataReader _reader;

            public DataReaderAsync(NpgsqlDataReader reader)
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
