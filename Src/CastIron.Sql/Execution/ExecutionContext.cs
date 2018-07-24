using System;
using System.Collections.Generic;
using System.Data;

namespace CastIron.Sql.Execution
{
    public class ExecutionContext : IExecutionContext, IContextBuilder
    {
        private IsolationLevel? _isolationLevel;
        private bool _aborted;

        public ExecutionContext(IDbConnection connection)
        {
            Connection = connection;
        }

        public IDbConnection Connection { get; }
        public IDbTransaction Transaction { get; private set; }
        public PerformanceMonitor Monitor { get; private set; }
        
        public void OpenConnection()
        {
            Connection.Open();
            if (_isolationLevel.HasValue)
                Transaction = Connection.BeginTransaction(_isolationLevel.Value);
        }

        public IDbCommand CreateCommand()
        {
            var command = Connection.CreateCommand();
            if (Transaction != null)
                command.Transaction = Transaction;
            return command;
        }

        public IContextBuilder UseTransaction(IsolationLevel il)
        {
            _isolationLevel = il;
            return this;
        }

        public IContextBuilder MonitorPerformance(Action<string> onReport)
        {
            Monitor = new PerformanceMonitor(onReport);
            return this;
        }

        public IContextBuilder MonitorPerformance(Action<IReadOnlyList<IPerformanceEntry>> onReport)
        {
            Monitor = new PerformanceMonitor(onReport);
            return this;
        }

        public void StartAction(string actionName)
        {
            Monitor?.StartEvent(actionName);
        }

        public void StartAction(int statementIndex, string actionName)
        {
            Monitor?.StartEvent($"Statement {statementIndex}: {actionName}");
        }

        public void MarkComplete()
        {
            Monitor?.Stop();
            Monitor?.PublishReport();
            if (Transaction != null)
            {
                if (_aborted)
                    Transaction.Rollback();
                else
                    Transaction.Commit();
                Transaction.Dispose();
            }
        }

        public void MarkAborted()
        {
            _aborted = true;
        }

        public void Dispose()
        {
            Connection?.Dispose();   
        }
    }
}