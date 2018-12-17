using System.Data;
using System.Data.SqlClient;
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

        public IDbConnection Create()
        {
            return new SqlConnection(_connectionString);
        }

        public IDbConnectionAsync CreateForAsync()
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

            public Task OpenAsync()
            {
                return _connection.OpenAsync();
            }

            public IDbCommandAsync CreateAsyncCommand()
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