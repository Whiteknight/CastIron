using System;
using System.Data;
using System.Threading;
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
        IDbConnectionAsync Create();
    }

    public interface IDbConnectionAsync : IDisposable
    {
        IDbConnection Connection { get; }
        Task OpenAsync(CancellationToken cancellationToken);
        IDbCommandAsync CreateCommand();
    }

    public interface IDbCommandAsync : IDisposable
    {
        IDbCommand Command { get; }
        IDataReaderAsync ExecuteReader();
        Task<IDataReaderAsync> ExecuteReaderAsync(CancellationToken cancellationToken);
        Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken);
    }

    public interface IDataReaderAsync : IDisposable
    {
        IDataReader Reader { get; }
        Task<bool> NextResultAsync(CancellationToken cancellationToken);
        Task<bool> ReadAsync(CancellationToken cancellationToken);
    }
}