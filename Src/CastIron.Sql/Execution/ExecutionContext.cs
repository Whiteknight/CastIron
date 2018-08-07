using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;

namespace CastIron.Sql.Execution
{
    public class ExecutionContext : IExecutionContext, IContextBuilder
    {
        private IsolationLevel? _isolationLevel;
        private bool _aborted;
        private int _completed;

        public ExecutionContext()
        {
            _completed = 0;
        }

        public IDbConnection Connection { get; private set; }
        public IDbTransaction Transaction { get; private set; }
        public PerformanceMonitor Monitor { get; private set; }
        public bool IsCompleted => Interlocked.CompareExchange(ref _completed, 0, 0) != 0;
        public string ConnectionStringName { get; private set; }
        
        public void SetConnection(IDbConnection connection)
        {
            Assert.ArgumentNotNull(connection, nameof(connection));
            Connection = connection;
        }

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

        public IContextBuilder UseConnection(string name)
        {
            ConnectionStringName = name;
            return this;
        }

        public IContextBuilder UseReadOnlyConnection()
        {
            ConnectionStringName = ConnectionStrings.ReadOnly;
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
            var isAlreadyComplete = Interlocked.CompareExchange(ref _completed, 1, 0) == 1;
            if (isAlreadyComplete)
                return;

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