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
        IContextBuilder UseTransaction(IsolationLevel il);
        IContextBuilder MonitorPerformance(Action<string> onReport);
        IContextBuilder MonitorPerformance(Action<IReadOnlyList<IPerformanceEntry>> onReport);
    }
}