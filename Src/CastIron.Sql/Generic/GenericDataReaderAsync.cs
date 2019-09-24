using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace CastIron.Sql.Generic
{
    public class GenericDataReaderAsync : IDataReaderAsync
    {
        private readonly IDataReader _reader;

        public GenericDataReaderAsync(IDataReader reader)
        {
            _reader = reader;
        }

        public void Dispose()
        {
            _reader?.Dispose();
        }

        public IDataReader Reader => _reader;

        public Task<bool> NextResultAsync(CancellationToken cancellationToken)
        {
            // TODO: Use reflection to try to find a suitable async method to call
            var ok = _reader.NextResult();
            return Task.FromResult(ok);
        }

        public Task<bool> ReadAsync(CancellationToken cancellationToken)
        {
            // TODO: Use reflection to try to find a suitable async method to call
            var ok = _reader.Read();
            return Task.FromResult(ok);
        }
    }
}