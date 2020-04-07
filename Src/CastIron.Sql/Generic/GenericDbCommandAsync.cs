using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace CastIron.Sql.Generic
{
    public sealed class GenericDbCommandAsync : IDbCommandAsync
    {
        public GenericDbCommandAsync(IDbCommand command)
        {
            Command = command;
        }

        public void Dispose() => Command.Dispose();

        public IDbCommand Command { get; }

        public IDataReaderAsync ExecuteReader() => new GenericDataReaderAsync(Command.ExecuteReader());

        public Task<IDataReaderAsync> ExecuteReaderAsync(CancellationToken cancellationToken)
        {
            var reader = new GenericDataReaderAsync(Command.ExecuteReader());
            return Task.FromResult<IDataReaderAsync>(reader);
        }

        public Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
        {
            var count = Command.ExecuteNonQuery();
            return Task.FromResult(count);
        }
    }
}
