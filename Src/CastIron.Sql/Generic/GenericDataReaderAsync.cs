﻿using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace CastIron.Sql.Generic
{
    public sealed class GenericDataReaderAsync : IDataReaderAsync
    {
        public GenericDataReaderAsync(IDataReader reader)
        {
            Reader = reader;
        }

        public void Dispose()
        {
            Reader?.Dispose();
        }

        public IDataReader Reader { get; }

        public Task<bool> NextResultAsync(CancellationToken cancellationToken)
        {
            // TODO: Use reflection to try to find a suitable async method to call
            var ok = Reader.NextResult();
            return Task.FromResult(ok);
        }

        public Task<bool> ReadAsync(CancellationToken cancellationToken)
        {
            // TODO: Use reflection to try to find a suitable async method to call
            var ok = Reader.Read();
            return Task.FromResult(ok);
        }
    }
}