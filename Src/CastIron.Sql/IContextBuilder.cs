using System;
using System.Collections.Generic;
using System.Data;

namespace CastIron.Sql
{
    public interface IContextBuilder
    {
        IContextBuilder UseTransaction(IsolationLevel il);
        IContextBuilder MonitorPerformance(Action<string> onReport);
        IContextBuilder MonitorPerformance(Action<IReadOnlyList<IPerformanceEntry>> onReport);
    }
}