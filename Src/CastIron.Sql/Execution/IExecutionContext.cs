using System;
using System.Data;

namespace CastIron.Sql.Execution
{
    /// <summary>
    /// Context for the database connection. Controls the lifetime of the connection and the options
    /// on it. This interface is intended for internal use only and should not be used directly.
    /// </summary>
    public interface IExecutionContext : IDisposable
    {
        IProviderConfiguration Provider { get; }
        IDbConnectionAsync Connection { get; }

        IDbTransaction Transaction { get; }

        IDbCommand CreateCommand();

        IDbCommandAsync CreateAsyncCommand();

        void StartAction(string actionName);
        void StartAction(int statementIndex, string actionName);
        void MarkComplete();
        void MarkAborted();
        bool IsCompleted { get; }
    }
}