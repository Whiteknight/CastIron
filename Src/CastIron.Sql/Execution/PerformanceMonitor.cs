using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CastIron.Sql.Execution
{
    public class PerformanceMonitor
    {
        private readonly Action<string> _onReportString;
        private readonly Action<IReadOnlyList<IPerformanceEntry>> _onReport;
        private readonly List<PerformanceEntry> _entries;
        private readonly Stopwatch _stopwatch;

        private PerformanceEntry _current;

        public PerformanceMonitor(Action<string> onReportString)
        {
            _onReportString = onReportString;
            _entries = new List<PerformanceEntry>();
            _stopwatch = new Stopwatch();
        }

        public PerformanceMonitor(Action<IReadOnlyList<IPerformanceEntry>> onReport)
        {
            _onReport = onReport;
            _entries = new List<PerformanceEntry>();
            _stopwatch = new Stopwatch();
        }

        private class PerformanceEntry : IPerformanceEntry
        {
            public PerformanceEntry(string name)
            {
                Name = name;
            }

            public string Name { get; }
            public double TimeMs { get; set; }
        }

        public void StartEvent(string name)
        {
            Stop();

            _current = new PerformanceEntry(name);
            _entries.Add(_current);
            _stopwatch.Start();
        }

        public void Stop()
        {
            if (_stopwatch.IsRunning)
            {
                _stopwatch.Stop();
                if (_current != null)
                    _current.TimeMs = _stopwatch.Elapsed.TotalMilliseconds;
                _stopwatch.Reset();
            }
        }

        public string GetReport()
        {
            Stop();
            return string.Join("\n", _entries.Select(e => $"{e.Name} took {e.TimeMs}ms"));
        }

        public void PublishReport()
        {
            _onReportString?.Invoke(GetReport());
            _onReport?.Invoke(_entries);
        }
    }
}