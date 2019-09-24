using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace CastIron.Sql.SqlServer
{
    /// <summary>
    /// Default IDbConnectionFactory implementation for Microsoft SQL Server and compatible databases
    /// </summary>
    public class SqlServerDbConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public SqlServerDbConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnectionAsync Create()
        {
            return new SqlServerDbConnectionAsync(new SqlConnection(_connectionString));
        }

        public class SqlServerDbConnectionAsync : IDbConnectionAsync
        {
            private readonly SqlConnection _connection;

            public SqlServerDbConnectionAsync(SqlConnection connection)
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
                return new SqlServerDbCommandAsync(_connection.CreateCommand());
            }

            public void Dispose()
            {
                _connection?.Dispose();
            }
        }

        public class SqlServerDbCommandAsync : IDbCommandAsync
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
                var reader = await _command.ExecuteReaderAsync(cancellationToken);
                return new SqlServerDataReaderAsync(reader);
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

        public class SqlServerDataReaderAsync : IDataReaderAsync
        {
            private readonly SqlDataReader _reader;

            public SqlServerDataReaderAsync(SqlDataReader reader)
            {
                _reader = reader;
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

            public void Dispose()
            {
                _reader?.Dispose();
            }
        }
    }
}