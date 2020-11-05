using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CastIron.Sql.Execution;

namespace CastIron.Sql
{
    /// <summary>
    /// Runner object to execute ISqlCommand and ISqlQuery variants. Creates a connection to the data 
    /// provider, executes commands and queries, and manages the return of results.
    /// </summary>
    public interface ISqlRunner
    {
        /// <summary>
        /// Information and configuration about the particular database provider
        /// </summary>
        IProviderConfiguration Provider { get; }

        /// <summary>
        /// A stringifier object which can be used to turn an ISqlCommand or ISqlQuery into human-readable
        /// SQL code for debugging and auditing purposes.
        /// </summary>
        QueryObjectStringifier ObjectStringifier { get; }

        /// <summary>
        /// Factory to create IDataInteraction objects to abstract details of the underlying connection
        /// and command objects
        /// </summary>
        IDataInteractionFactory InteractionFactory { get; }

        IMapCompilerSource MapCompiler { get; }
        IMapCache MapCache { get; }

        /// <summary>
        /// Create an execution context for a database connection. The context provides parameters
        /// and utilities to help manage the connection
        /// </summary>
        /// <returns></returns>
        ExecutionContext CreateExecutionContext();

        /// <summary>
        /// Open a connection to the database and execute a list of executors on the open connection
        /// </summary>
        /// <param name="executors"></param>
        void Execute(IReadOnlyList<Action<IExecutionContext, int>> executors);

        /// <summary>
        /// Open a connection to the database and execute an executor on the open connection. The
        /// executor is expected to return a result
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="executor"></param>
        /// <returns></returns>
        T Execute<T>(Func<IExecutionContext, T> executor);

        /// <summary>
        /// Open a connection to the database and execute an executor on the open connection. The
        /// executor is not expected to return a result
        /// </summary>
        /// <param name="executor"></param>
        void Execute(Action<IExecutionContext> executor);

        /// <summary>
        /// Open a connection to the database and execute an executor on the open connection
        /// asynchronously.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="executor"></param>
        /// <returns></returns>
        Task<T> ExecuteAsync<T>(Func<IExecutionContext, Task<T>> executor);

        /// <summary>
        /// Open a connection to the database and execute an executor on the open connection
        /// asynchronously. The executor is not expected to return a result.
        /// </summary>
        /// <param name="executor"></param>
        /// <returns></returns>
        Task ExecuteAsync(Func<IExecutionContext, Task> executor);
    }
}