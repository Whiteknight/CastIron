using System;
using System.Data;
using System.Threading.Tasks;

namespace CastIron.Sql
{
    /// <summary>
    /// A factory for IDbConnection objects. Implementations of this class allow CastIron to target different
    /// SQL providers such as MS SQL Server, MySQL, MariaDB or Oracle SQL if they implement the necessary
    /// abstractions.
    /// </summary>
    public interface IDbConnectionFactory
    {
        /// <summary>
        /// Creates the new IDbConnection for the current provider
        /// </summary>
        /// <returns></returns>
        IDbConnection Create();

        IDbConnectionAsync CreateForAsync();
    }

    public interface IDbConnectionAsync : IDisposable
    {
        IDbConnection Connection { get; }
        Task OpenAsync();
        IDbCommandAsync CreateAsyncCommand();

    }

    public interface IDbCommandAsync : IDisposable
    {
        IDbCommand Command { get; }
        Task<IDataReader> ExecuteReaderAsync();
        Task<int> ExecuteNonQueryAsync();
    }
}