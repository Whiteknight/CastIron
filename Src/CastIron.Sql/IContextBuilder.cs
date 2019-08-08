using System;
using System.Collections.Generic;
using System.Data;

namespace CastIron.Sql
{
    /// <summary>
    /// Build and setup the execution context for the db connection. Can be used to set options
    /// and additional details for the connection 
    /// </summary>
    public interface IContextBuilder
    {
        /// <summary>
        /// Use a transaction with the given isolation level for every call to .Query or .Execute
        /// A new transaction is created for each call to .Execute or .Query with the given
        /// </summary>
        /// <param name="il"></param>
        /// <returns></returns>
        IContextBuilder UseTransaction(IsolationLevel il);

        /// <summary>
        /// Monitor the performance of the batch and send a stringified performance report to the
        /// given callback at the end of execution of every .Execute or .Query call
        /// </summary>
        /// <param name="onReport"></param>
        /// <returns></returns>
        IContextBuilder MonitorPerformance(Action<string> onReport);

        /// <summary>
        /// Monitor the performance of the batch and send the raw performance data to the given
        /// callback at the end of execution of every .Execute or .Query call
        /// </summary>
        /// <param name="onReport"></param>
        /// <returns></returns>
        IContextBuilder MonitorPerformance(Action<IReadOnlyList<IPerformanceEntry>> onReport);

        /// <summary>
        /// Perform a callback on the command text before execution
        /// </summary>
        /// <param name="onCommand"></param>
        /// <returns></returns>
        IContextBuilder ViewCommandBeforeExecution(Action<string> onCommand);

        /// <summary>
        /// Set the timeout for commands. This value may be overridden by the user in other
        /// locations. Notice that this is the command timeout, not the connection timeout.
        /// Connection timeouts can only be set in the connection string (if supported by the
        /// provider). Default timeout varies by provider but is probably about 30 seconds.
        /// </summary>
        /// <param name="seconds"></param>
        /// <returns></returns>
        IContextBuilder SetTimeoutSeconds(int seconds);

        /// <summary>
        /// Set the timeout for commands. This value may be overridden by the user in other
        /// locations. Notice that this is the command timeout, not the connection timeout.
        /// Connection timeouts can only be set in the connection string (if supported by the
        /// provider). Default timeout varies by provider but is probably about 30 seconds.
        /// </summary>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        IContextBuilder SetTimeout(TimeSpan timeSpan);
    }
}