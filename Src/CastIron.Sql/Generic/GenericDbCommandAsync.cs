using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CastIron.Sql.Generic
{
    public class GenericDbCommandAsync : IDbCommandAsync
    {
        private readonly IDbCommand _command;

        public GenericDbCommandAsync(IDbCommand command)
        {
            _command = command;
        }

        public void Dispose()
        {
            _command.Dispose();
        }

        public IDbCommand Command => _command;

        public IDataReaderAsync ExecuteReader()
        {
            return new GenericDataReaderAsync(_command.ExecuteReader());
        }

        public Task<IDataReaderAsync> ExecuteReaderAsync(CancellationToken cancellationToken)
        {
            // TODO: Use reflection to try to find a suitable async method to call
            var reader = new GenericDataReaderAsync(_command.ExecuteReader());
            return Task.FromResult<IDataReaderAsync>(reader);
        }

        public Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
        {
            // TODO: Use reflection to try to find a suitable async method to call
            var count = _command.ExecuteNonQuery();
            return Task.FromResult(count);
        }
    }
}
