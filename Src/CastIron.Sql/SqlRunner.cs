﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CastIron.Sql.Execution;
using CastIron.Sql.Utility;
using ExecutionContext = CastIron.Sql.Execution.ExecutionContext;

namespace CastIron.Sql
{
    /// <summary>
    /// Runner for Query and Command objects. 
    /// </summary>
    public class SqlRunner : ISqlRunner
    {
        private readonly Action<IContextBuilder> _defaultBuilder;
        private readonly SqlRunnerCore _core;
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IMapCompilerSource _compilerBuilder;
        private readonly IMapCache _mapCache;

        public SqlRunner(SqlRunnerCore core, IDbConnectionFactory connectionFactory, Action<IContextBuilder> defaultBuilder, IMapCompilerSource compilerBuilder, IMapCache mapCache)
        {
            Argument.NotNull(core, nameof(core));
            Argument.NotNull(connectionFactory, nameof(connectionFactory));
            Argument.NotNull(compilerBuilder, nameof(compilerBuilder));
            Argument.NotNull(mapCache, nameof(mapCache));

            _core = core;
            _connectionFactory = connectionFactory;
            _defaultBuilder = defaultBuilder;
            _compilerBuilder = compilerBuilder;
            _mapCache = mapCache;
        }

        public IDataInteractionFactory InteractionFactory => _core.InteractionFactory;

        public IProviderConfiguration Provider => _core.Provider;

        public IMapCompilerSource MapCompiler => _compilerBuilder;

        public IMapCache MapCache => _mapCache;

        public QueryObjectStringifier ObjectStringifier => _core.ObjectStringifier;

        public ExecutionContext CreateExecutionContext()
        {
            var compiler = _compilerBuilder.GetCompiler();
            var context = new ExecutionContext(_connectionFactory, Provider, _core.CommandStringifier, compiler, _mapCache);
            _defaultBuilder?.Invoke(context);
            return context;
        }

        public void Execute(IReadOnlyList<Action<IExecutionContext, int>> executors)
        {
            using var context = CreateExecutionContext();
            _core.Execute(context, executors);
        }

        public T Execute<T>(Func<IExecutionContext, T> executor)
        {
            using var context = CreateExecutionContext();
            return _core.Execute(context, executor);
        }

        public void Execute(Action<IExecutionContext> executor)
        {
            using var context = CreateExecutionContext();
            _core.Execute(context, executor);
        }

        public async Task<T> ExecuteAsync<T>(Func<IExecutionContext, Task<T>> executor)
        {
            using var context = CreateExecutionContext();
            return await _core.ExecuteAsync(context, executor).ConfigureAwait(false);
        }

        public async Task ExecuteAsync(Func<IExecutionContext, Task> executor)
        {
            using var context = CreateExecutionContext();
            await _core.ExecuteAsync(context, executor).ConfigureAwait(false);
        }
    }
}