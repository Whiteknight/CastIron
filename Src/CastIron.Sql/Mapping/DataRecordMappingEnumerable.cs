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
        private readonly IDataReaderAsync _reader;
        private readonly IExecutionContext _context;
        private readonly Func<IDataRecord, T> _map;
        private int _readAttempts;

        public DataRecordMappingEnumerable(IDataReaderAsync reader, IExecutionContext context, Func<IDataRecord, T> map)
        {
            Argument.NotNull(reader, nameof(reader));
            Argument.NotNull(context, nameof(context));
            Argument.NotNull(map, nameof(map));

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
                throw DataReaderException.ResultSetReadMoreThanOnce();
            if ((_context != null && _context.IsCompleted) || _reader.Reader.IsClosed)
                throw DataReaderException.ReaderClosed();
            return new ResultSetEnumerator(_reader, _context, _map);
        }

        private class ResultSetEnumerator : IEnumerator<T>
        {
            private readonly IDataReaderAsync _reader;
            private readonly IExecutionContext _context;
            private readonly Func<IDataRecord, T> _read;

            public ResultSetEnumerator(IDataReaderAsync reader, IExecutionContext context, Func<IDataRecord, T> read)
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
                // TODO: We need to keep track of the result set so that if the reader has advanced we cannot continue
                // TODO: While this is enumerating we cannot do anything else with the reader.
                if (_context.IsCompleted || _reader.Reader.IsClosed)
                {
                    Current = default(T);
                    return false;
                }

                var ok = _reader.Reader.Read();
                Current = ok ? _read(_reader.Reader) : default(T);
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