using System;
using System.Data;

namespace CastIron.Sql.Execution
{
    /// <summary>
    /// Context for the database connection. Controls the lifetime of the connection and the options
    /// on it.
    /// </summary>
    public interface IExecutionContext : IDisposable
    {
        IDbConnection Connection { get; }

        IDbTransaction Transaction { get; }

        IDbCommand CreateCommand();

        void StartAction(string actionName);
        void StartAction(int statementIndex, string actionName);
        void MarkComplete();
        void MarkAborted();
        bool IsCompleted { get; }
    }
}