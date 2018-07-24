using System;
using System.Data;

namespace CastIron.Sql.Execution
{
    public interface IExecutionContext : IDisposable
    {
        IDbConnection Connection { get; }

        IDbTransaction Transaction { get; }

        IDbCommand CreateCommand();

        void StartAction(string actionName);
        void StartAction(int statementIndex, string actionName);
        void MarkComplete();
        void MarkAborted();
    }
}