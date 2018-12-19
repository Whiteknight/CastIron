using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace CastIron.Sql.Execution
{
    public class ExecutionContext : IExecutionContext, IContextBuilder
    {
        private IsolationLevel? _isolationLevel;
        private bool _aborted;
        private int _completed;
        private int _opened;

        public ExecutionContext(IDbConnectionFactory factory)
        {
            _completed = 0;
            _opened = 0;
            Connection = factory.CreateForAsync();
        }

        public IDbConnectionAsync Connection { get; }
        public IDbTransaction Transaction { get; private set; }
        public PerformanceMonitor Monitor { get; private set; }
        public bool IsCompleted => Interlocked.CompareExchange(ref _completed, 0, 0) != 0;
        public bool IsOpen => Interlocked.CompareExchange(ref _opened, 0, 0) != 0;

        private void MarkOpened()
        {
            if (Interlocked.CompareExchange(ref _opened, 1, 0) != 0)
                throw new Exception("Connection is already open and cannot be opened again");
        }
        
        public void OpenConnection()
        {
            MarkOpened();
            Connection.Connection.Open();
            if (_isolationLevel.HasValue)
                Transaction = Connection.Connection.BeginTransaction(_isolationLevel.Value);
        }

        public async Task OpenConnectionAsync()
        {
            MarkOpened();
            await Connection.OpenAsync();
            if (_isolationLevel.HasValue)
                Transaction = Connection.Connection.BeginTransaction(_isolationLevel.Value);
        }

        public IDbCommand CreateCommand()
        {
            if (!IsOpen)
                throw new Exception("Cannot create a command when the connection is not open");
            var command = Connection.Connection.CreateCommand();
            if (Transaction != null)
                command.Transaction = Transaction;
            return command;
        }

        public IDbCommandAsync CreateAsyncCommand()
        {
            if (!IsOpen)
                throw new Exception("Cannot create a command when the connection is not open");
            var command = Connection.CreateAsyncCommand();
            if (Transaction != null)
                command.Command.Transaction = Transaction;
            return command;
        }

        public IContextBuilder UseTransaction(IsolationLevel il)
        {
            if (IsOpen)
                throw new Exception("Cannot set transaction or isolation level when the connection is already opened");
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

            Connection?.Connection?.Close();
            Interlocked.CompareExchange(ref _opened, 0, 1);
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