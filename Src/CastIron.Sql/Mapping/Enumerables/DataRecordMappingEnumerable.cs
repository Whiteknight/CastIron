using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using CastIron.Sql.Execution;
using CastIron.Sql.Utility;

namespace CastIron.Sql.Mapping.Enumerables
{
    /// <summary>
    /// IEnumerable implementation to apply a map function to every row in an IDataReader
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class DataRecordMappingEnumerable<T> : IEnumerable<T>
    {
        private readonly IDataResultsBase _results;
        private readonly IDataReaderAsync _reader;
        private readonly IExecutionContext _context;
        private readonly Func<IDataRecord, T> _map;
        private readonly int _currentSet;
        private int _readAttempts;

        public DataRecordMappingEnumerable(IDataResultsBase results, IDataReaderAsync reader, IExecutionContext context, Func<IDataRecord, T> map)
        {
            Argument.NotNull(reader, nameof(reader));
            Argument.NotNull(context, nameof(context));
            Argument.NotNull(map, nameof(map));
            _results = results;
            _reader = reader;
            _context = context;
            _map = map;
            _currentSet = results.CurrentSet;
            _readAttempts = 0;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<T> GetEnumerator()
        {
            var attempt = Interlocked.Increment(ref _readAttempts);
            if (attempt > 1)
                throw DataReaderException.ResultSetReadMoreThanOnce();
            if ((_context != null && _context.IsCompleted) || _reader.Reader.IsClosed)
                throw DataReaderException.ReaderClosed();
            return new ResultSetEnumerator(_results, _reader, _context, _map, _currentSet);
        }

        private sealed class ResultSetEnumerator : IEnumerator<T>
        {
            private readonly IDataResultsBase _results;
            private readonly IDataReaderAsync _reader;
            private readonly IExecutionContext _context;
            private readonly Func<IDataRecord, T> _read;
            private readonly int _set;

            public ResultSetEnumerator(IDataResultsBase results, IDataReaderAsync reader, IExecutionContext context, Func<IDataRecord, T> read, int set)
            {
                _results = results;
                _reader = reader;
                _context = context;
                _read = read;
                _set = set;
            }

            public void Dispose()
            {
                // Nothing to dispose here. We can't dispose the underlying reader, because we
                // might create a new enumerator from the next result set
            }

            public bool MoveNext()
            {
                if (_context.IsCompleted || _reader.Reader.IsClosed)
                {
                    Current = default;
                    return false;
                }
                if (_results.CurrentSet != _set)
                {
                    Current = default;
                    return false;
                }

                var ok = _reader.Reader.Read();
                Current = ok ? _read(_reader.Reader) : default;
                return ok;
            }

            public void Reset() => throw new NotImplementedException();

            public T Current { get; private set; }

            object IEnumerator.Current => Current;
        }
    }
}