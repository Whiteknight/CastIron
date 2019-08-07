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
        /// Use a transaction with the given isolation level to encapsulate all statements in the
        /// batch
        /// </summary>
        /// <param name="il"></param>
        /// <returns></returns>
        IContextBuilder UseTransaction(IsolationLevel il);

        /// <summary>
        /// Monitor the performance of the batch and send a stringified performance report to the given callback
        /// </summary>
        /// <param name="onReport"></param>
        /// <returns></returns>
        IContextBuilder MonitorPerformance(Action<string> onReport);

        /// <summary>
        /// Monitor the performance of the batch and send the raw performance data to the given callback
        /// </summary>
        /// <param name="onReport"></param>
        /// <returns></returns>
        IContextBuilder MonitorPerformance(Action<IReadOnlyList<IPerformanceEntry>> onReport);

        /// <summary>
        /// Set the timeout for the connection. This value may be overridden by the user in other
        /// locations.
        /// </summary>
        /// <param name="seconds"></param>
        /// <returns></returns>
        IContextBuilder SetTimeoutSeconds(int seconds);

        /// <summary>
        /// Set the timeout for the connection. This value may be overridden by the user in other
        /// locations.
        /// </summary>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        IContextBuilder SetTimeout(TimeSpan timeSpan);
    }
}