using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using CastIron.Sql.Execution;
using CastIron.Sql.Utility;

namespace CastIron.Sql.Mapping
{
    public class DataRecordMappingEnumerable<T> : IEnumerable<T>
    {
        private readonly IDataReader _reader;
        private readonly IExecutionContext _context;
        private readonly Func<IDataRecord, T> _map;
        private int _readAttempts;

        public DataRecordMappingEnumerable(IDataReader reader, IExecutionContext context, Func<IDataRecord, T> map)
        {
            Assert.ArgumentNotNull(reader, nameof(reader));
            Assert.ArgumentNotNull(context, nameof(context));
            Assert.ArgumentNotNull(map, nameof(map));

            _reader = reader;
            _context = context;
            _map = map;
            _readAttempts = 0;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            var attempt = Interlocked.Increment(ref _readAttempts);
            if (attempt > 1)
                throw new Exception("Cannot read the same result set more than once. Please cache your results and read from the cache");
            if (_context != null && _context.IsCompleted)
                throw new Exception("The connection is closed and the result set cannot be read");
            return new ResultSetEnumerator(_reader, _context, _map);
        }

        private class ResultSetEnumerator : IEnumerator<T>
        {
            private readonly IDataReader _reader;
            private readonly IExecutionContext _context;
            private readonly Func<IDataRecord, T> _read;

            public ResultSetEnumerator(IDataReader reader, IExecutionContext context, Func<IDataRecord, T> read)
            {
                _reader = reader;
                _context = context;
                _read = read;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (_context.IsCompleted)
                {
                    Current = default(T);
                    return false;
                }

                var ok = _reader.Read();
                Current = ok ? _read(_reader) : default(T);
                return ok;
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }

            public T Current { get; private set; }

            object IEnumerator.Current => Current;
        }        
    }
}