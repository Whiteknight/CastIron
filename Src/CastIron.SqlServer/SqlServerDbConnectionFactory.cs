using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using CastIron.Sql;
using CastIron.Sql.Utility;

namespace CastIron.SqlServer
{
    /// <summary>
    /// Default IDbConnectionFactory implementation for Microsoft SQL Server and compatible databases
    /// </summary>
    public class SqlServerDbConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public SqlServerDbConnectionFactory(string connectionString)
        {
            Argument.NotNullOrEmpty(connectionString, nameof(connectionString));
            _connectionString = connectionString;
        }

        public IDbConnectionAsync Create() => new SqlServerDbConnectionAsync(new SqlConnection(_connectionString));

        public sealed class SqlServerDbConnectionAsync : IDbConnectionAsync
        {
            private readonly SqlConnection _connection;

            public SqlServerDbConnectionAsync(SqlConnection connection)
            {
                _connection = connection;
            }

            public IDbConnection Connection => _connection;

            public Task OpenAsync(CancellationToken cancellationToken) => _connection.OpenAsync(cancellationToken);

            public IDbCommandAsync CreateCommand() => new SqlServerDbCommandAsync(_connection.CreateCommand());

            public void Dispose() => _connection?.Dispose();
        }

        public sealed class SqlServerDbCommandAsync : IDbCommandAsync
        {
            private readonly SqlCommand _command;

            public SqlServerDbCommandAsync(SqlCommand command)
            {
                _command = command;
            }

            public IDbCommand Command => _command;

            public IDataReaderAsync ExecuteReader()
            {
                var reader = _command.ExecuteReader();
                return new SqlServerDataReaderAsync(reader);
            }

            public async Task<IDataReaderAsync> ExecuteReaderAsync(CancellationToken cancellationToken)
            {
                var reader = await _command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                return new SqlServerDataReaderAsync(reader);
            }

            public Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) => _command.ExecuteNonQueryAsync(cancellationToken);

            public void Dispose() => _command?.Dispose();
        }

        public sealed class SqlServerDataReaderAsync : IDataReaderAsync
        {
            private readonly SqlDataReader _reader;

            public SqlServerDataReaderAsync(SqlDataReader reader)
            {
                _reader = reader;
            }

            public IDataReader Reader => _reader;

            public Task<bool> NextResultAsync(CancellationToken cancellationToken) => _reader.NextResultAsync(cancellationToken);

            public Task<bool> ReadAsync(CancellationToken cancellationToken) => _reader.ReadAsync(cancellationToken);

            public void Dispose() => _reader?.Dispose();
        }
    }
}