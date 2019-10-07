using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CastIron.Sql.Execution;
using CastIron.Sql.Utility;
using ExecutionContext = CastIron.Sql.Execution.ExecutionContext;

namespace CastIron.Sql
{
    /// <summary>
    /// Core runner functionality which implements queries but does not manage the initialization
    /// or lifetime of Execution context.
    /// This class is not intended to be used directly. Use SqlRunner instead.
    /// </summary>
    public class SqlRunnerCore
    {
        // The runner core and all objects managed by it should be immutable and have no mutable
        // state. This object can and should be cached statically

        public SqlRunnerCore(IDataInteractionFactory interactionFactory, IProviderConfiguration providerConfiguration, IDbCommandStringifier stringifier)
        {
            Argument.NotNull(interactionFactory, nameof(interactionFactory));
            Argument.NotNull(providerConfiguration, nameof(providerConfiguration));
            Argument.NotNull(stringifier, nameof(stringifier));

            Provider = providerConfiguration;
            InteractionFactory = interactionFactory;
            CommandStringifier = stringifier;
            ObjectStringifier = new QueryObjectStringifier(CommandStringifier, InteractionFactory);
        }

        public IDataInteractionFactory InteractionFactory { get; }
        public IProviderConfiguration Provider { get; }
        public QueryObjectStringifier ObjectStringifier { get; }
        public IDbCommandStringifier CommandStringifier { get; }

        public void Execute(ExecutionContext context, IReadOnlyList<Action<IExecutionContext, int>> executors)
        {
            Argument.NotNull(executors, nameof(executors));
            context.OpenConnection();
            for (var i = 0; i < executors.Count; i++)
                executors[i](context, i);
            context.MarkComplete();
        }

        public T Execute<T>(ExecutionContext context, Func<IExecutionContext, T> executor)
        {
            Argument.NotNull(executor, nameof(executor));
            context.OpenConnection();
            var result = executor(context);
            context.MarkComplete();
            return result;
        }

        public async Task<T> ExecuteAsync<T>(ExecutionContext context, Func<IExecutionContext, Task<T>> executor, CancellationToken cancellationToken = new CancellationToken())
        {
            Argument.NotNull(executor, nameof(executor));
            await context.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            var result = await executor(context).ConfigureAwait(false);
            context.MarkComplete();
            return result;
        }

        public void Execute(ExecutionContext context, Action<IExecutionContext> executor)
        {
            Argument.NotNull(executor, nameof(executor));
            context.OpenConnection();
            executor(context);
            context.MarkComplete();
        }

        public async Task ExecuteAsync(ExecutionContext context, Func<IExecutionContext, Task> executor, CancellationToken cancellationToken = new CancellationToken())
        {
            Argument.NotNull(executor, nameof(executor));
            await context.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await executor(context).ConfigureAwait(false);
            context.MarkComplete();
        }
    }
}