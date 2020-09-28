using System;
using System.Data;
using CastIron.Sql.Mapping;

namespace CastIron.Sql.Execution
{
    /// <summary>
    /// Context for the database connection. Controls the lifetime of the connection and the options
    /// on it. This interface is intended for internal use only and should not be used directly.
    /// </summary>
    public interface IExecutionContext : IDisposable
    {
        IDbCommandStringifier Stringifier { get; }
        IProviderConfiguration Provider { get; }
        IDbConnectionAsync Connection { get; }

        IDbTransaction Transaction { get; }

        IDbCommandAsync CreateCommand();

        void StartSetupCommand(int index);
        void StartExecute(int index, IDbCommandAsync command);
        void StartMapResults(int index);
        void MarkComplete();
        void MarkAborted();
        bool IsCompleted { get; }

        IMapCompiler MapCompiler { get; }
    }
}