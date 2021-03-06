﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using CastIron.Sql.Mapping;

namespace CastIron.Sql.Execution
{
    public sealed class ExecutionContext : IExecutionContext, IContextBuilder
    {
        private IsolationLevel? _isolationLevel;
        private bool _aborted;
        private int _completed;
        private int _opened;
        private int _timeoutSeconds;
        private PerformanceMonitor _monitor;
        private Action<string> _onCommandText;

        public ExecutionContext(IDbConnectionFactory factory, IProviderConfiguration provider, IDbCommandStringifier stringifier, IMapCompiler compiler, IMapCache mapCache)
        {
            MapCompiler = compiler;
            Stringifier = stringifier;
            Provider = provider;
            MapCache = mapCache;
            _completed = 0;
            _opened = 0;
            Connection = factory.Create();
        }

        public IDbCommandStringifier Stringifier { get; }
        public IProviderConfiguration Provider { get; }
        public IDbConnectionAsync Connection { get; }
        public IDbTransaction Transaction { get; private set; }
        public IMapCompiler MapCompiler { get; }
        public IMapCache MapCache { get; }

        public bool IsCompleted => Interlocked.CompareExchange(ref _completed, 0, 0) != 0;
        public bool IsOpen => Interlocked.CompareExchange(ref _opened, 0, 0) != 0;

        private void MarkOpened()
        {
            if (Interlocked.CompareExchange(ref _opened, 1, 0) != 0)
                throw ExecutionContextException.ConnectionAlreadyOpen();
        }

        public void OpenConnection()
        {
            _monitor?.StartEvent("Open Connection");
            MarkOpened();
            Connection.Connection.Open();
            if (_isolationLevel.HasValue)
                Transaction = Connection.Connection.BeginTransaction(_isolationLevel.Value);
        }

        public async Task OpenConnectionAsync(CancellationToken cancellationToken)
        {
            _monitor?.StartEvent("Open Connection");
            MarkOpened();
            await Connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            if (_isolationLevel.HasValue)
                Transaction = Connection.Connection.BeginTransaction(_isolationLevel.Value);
        }

        public IDbCommandAsync CreateCommand()
        {
            if (!IsOpen)
                throw ExecutionContextException.ConnectionNotOpen();
            var command = Connection.CreateCommand();
            if (Transaction != null)
                command.Command.Transaction = Transaction;
            // Set this, even though in async contexts it will probably be ignored
            if (_timeoutSeconds > 0)
                command.Command.CommandTimeout = _timeoutSeconds;
            return command;
        }

        public IContextBuilder UseTransaction(IsolationLevel il)
        {
            if (IsOpen)
                throw ExecutionContextException.ConnectionAlreadyOpen();
            _isolationLevel = il;
            return this;
        }

        public IContextBuilder MonitorPerformance(Action<string> onReport)
        {
            _monitor = new PerformanceMonitor(onReport);
            return this;
        }

        public IContextBuilder MonitorPerformance(Action<IReadOnlyList<IPerformanceEntry>> onReport)
        {
            _monitor = new PerformanceMonitor(onReport);
            return this;
        }

        public IContextBuilder ViewCommandBeforeExecution(Action<string> onCommand)
        {
            _onCommandText = onCommand;
            return this;
        }

        public IContextBuilder SetTimeoutSeconds(int seconds)
        {
            _timeoutSeconds = seconds;
            return this;
        }

        public IContextBuilder SetTimeout(TimeSpan timeSpan) => SetTimeoutSeconds((int)timeSpan.TotalSeconds);

        public void StartSetupCommand(int index)
        {
            _monitor?.StartEvent($"Statement {index}: Setup Command");
        }

        public void StartExecute(int index, IDbCommandAsync command)
        {
            _monitor?.StartEvent($"Statement {index}: Execute");
            if (_onCommandText != null)
            {
                var text = Stringifier.Stringify(command);
                _onCommandText(text);
            }
        }

        public void StartMapResults(int index)
        {
            _monitor?.StartEvent($"Statement {index}: Map Results");
        }

        public void MarkComplete()
        {
            var isAlreadyComplete = Interlocked.CompareExchange(ref _completed, 1, 0) == 1;
            if (isAlreadyComplete)
                return;

            _monitor?.Stop();
            _monitor?.PublishReport();
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