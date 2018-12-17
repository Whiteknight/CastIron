using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace CastIron.Sql.Execution
{
    public class ExecutionContext : IExecutionContext, IContextBuilder
    {
        private readonly IDbConnectionFactory _factory;
        private IsolationLevel? _isolationLevel;
        private bool _aborted;
        private int _completed;

        public ExecutionContext(IDbConnectionFactory factory)
        {
            _factory = factory;
            _completed = 0;
        }

        private IDbConnection _connection;
        public IDbConnection Connection => _connection ?? (_connection = _factory.Create());

        private IDbConnectionAsync _connectionAsync;
        public IDbConnectionAsync ConnectionAsync => _connectionAsync ?? (_connectionAsync = _factory.CreateForAsync());
        public IDbTransaction Transaction { get; private set; }
        public PerformanceMonitor Monitor { get; private set; }
        public bool IsCompleted => Interlocked.CompareExchange(ref _completed, 0, 0) != 0;
        
        public void OpenConnection()
        {
            Connection.Open();
            if (_isolationLevel.HasValue)
                Transaction = Connection.BeginTransaction(_isolationLevel.Value);
        }

        public async Task OpenConnectionAsync()
        {
            await ConnectionAsync.OpenAsync();
            if (_isolationLevel.HasValue)
                Transaction = ConnectionAsync.Connection.BeginTransaction(_isolationLevel.Value);
        }

        public IDbCommand CreateCommand()
        {
            var command = Connection.CreateCommand();
            if (Transaction != null)
                command.Transaction = Transaction;
            return command;
        }

        public IDbCommandAsync CreateAsyncCommand()
        {
            var command = ConnectionAsync.CreateAsyncCommand();
            if (Transaction != null)
                command.Command.Transaction = Transaction;
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
            _connection?.Dispose();
            _connectionAsync?.Dispose();
        }
    }
}