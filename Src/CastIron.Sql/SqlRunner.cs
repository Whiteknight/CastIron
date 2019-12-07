using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CastIron.Sql.Execution;
using CastIron.Sql.Mapping;
using CastIron.Sql.Utility;

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
        private readonly List<IScalarMapCompiler> _customScalarMapCompilers;
        private IMapCompiler _defaultCompiler;

        public SqlRunner(SqlRunnerCore core, IDbConnectionFactory connectionFactory, Action<IContextBuilder> defaultBuilder)
        {
            Argument.NotNull(core, nameof(core));
            Argument.NotNull(connectionFactory, nameof(connectionFactory));

            _core = core;
            _connectionFactory = connectionFactory;
            _defaultBuilder = defaultBuilder;
            _customScalarMapCompilers = new List<IScalarMapCompiler>();
        }

        public IDataInteractionFactory InteractionFactory => _core.InteractionFactory;

        public IProviderConfiguration Provider => _core.Provider;

        public QueryObjectStringifier ObjectStringifier => _core.ObjectStringifier;

        public ExecutionContext CreateExecutionContext()
        {
            var compiler = GetCompiler();
            var context = new ExecutionContext(_connectionFactory, Provider, _core.CommandStringifier, compiler);
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

        public void AddCustomScalarMapCompiler(IScalarMapCompiler compiler)
        {
            _defaultCompiler = null;
            _customScalarMapCompilers.Add(compiler);
        }

        public void ClearCustomScalarMapCompilers()
        {
            if (_customScalarMapCompilers.Count > 0)
            {
                _defaultCompiler = null;
                _customScalarMapCompilers.Clear();
            }
        }

        private IMapCompiler GetCompiler()
        {
            if (_defaultCompiler != null)
                return _defaultCompiler;
            if (_customScalarMapCompilers.Count == 0)
                return MapCompiler.GetDefaultInstance();

            var defaultScalarCompilers = MapCompilation.GetDefaultScalarMapCompilers();
            var allScalarCompilers = _customScalarMapCompilers.Concat(defaultScalarCompilers);
            _defaultCompiler = new MapCompiler(allScalarCompilers);
            return _defaultCompiler;
        }
    }
}