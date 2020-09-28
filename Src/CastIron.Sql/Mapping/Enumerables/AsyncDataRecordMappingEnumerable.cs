#if NETSTANDARD2_1
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using CastIron.Sql.Execution;
using CastIron.Sql.Utility;

namespace CastIron.Sql.Mapping.Enumerables
{
    public class AsyncDataRecordMappingEnumerable<T> : IAsyncEnumerable<T>
    {
        private readonly IDataReaderAsync _reader;
        private readonly IExecutionContext _context;
        private readonly Func<IDataRecord, T> _map;
        private int _readAttempts;

        public AsyncDataRecordMappingEnumerable(IDataReaderAsync reader, IExecutionContext context, Func<IDataRecord, T> map)
        {
            Argument.NotNull(reader, nameof(reader));
            Argument.NotNull(context, nameof(context));
            Argument.NotNull(map, nameof(map));

            _reader = reader;
            _context = context;
            _map = map;
            _readAttempts = 0;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken())
        {
            var attempt = Interlocked.Increment(ref _readAttempts);
            if (attempt > 1)
                throw DataReaderException.ResultSetReadMoreThanOnce();
            if (_context?.IsCompleted == true || _reader.Reader.IsClosed)
                throw DataReaderException.ReaderClosed();
            return new ResultSetEnumerator(_reader, _context, _map);
        }

        private class ResultSetEnumerator : IAsyncEnumerator<T>
        {
            private readonly IDataReaderAsync _reader;
            private readonly IExecutionContext _context;
            private readonly Func<IDataRecord, T> _map;

            public ResultSetEnumerator(IDataReaderAsync reader, IExecutionContext context, Func<IDataRecord, T> map)
            {
                _reader = reader;
                _context = context;
                _map = map;
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (_context.IsCompleted || _reader.Reader.IsClosed)
                {
                    Current = default;
                    return false;
                }

                var ok = await _reader.ReadAsync(new CancellationToken()).ConfigureAwait(false);
                Current = ok ? _map(_reader.Reader) : default;
                return ok;
            }

            public ValueTask DisposeAsync() => new ValueTask(Task.CompletedTask);

            public T Current { get; private set; }
        }
    }
}
#endif