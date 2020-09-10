using CastIron.Sql.Execution;
using CastIron.Sql.Utility;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace CastIron.Sql
{
    /// <summary>
    /// Facilitates pretty-printing of commands for the purposes of debugging, logging and auditing
    /// Notice that the stringified commands may not be complete or valid SQL in some situations
    /// </summary>
    public class QueryObjectStringifier
    {
        private readonly IDataInteractionFactory _interactionFactory;
        private readonly IDbCommandStringifier _stringifier;

        public QueryObjectStringifier(IDbCommandStringifier commandStringifier, IDataInteractionFactory interactionFactory)
        {
            Argument.NotNull(commandStringifier, nameof(commandStringifier));
            Argument.NotNull(interactionFactory, nameof(interactionFactory));

            _interactionFactory = interactionFactory;
            _stringifier = commandStringifier;
        }

        public string Stringify<T>(ISqlQuerySimple<T> query)
        {
            var dummy = new DummyCommandAsync(new SqlCommand());
            new SqlQuerySimpleStrategy().SetupCommand(query, dummy);
            return _stringifier.Stringify(dummy);
        }

        public string Stringify(ISqlCommand command)
        {
            var dummy = new DummyCommandAsync(new SqlCommand());
            new SqlCommandStrategy(_interactionFactory).SetupCommand(command, dummy);
            return _stringifier.Stringify(dummy);
        }

        public string Stringify<T>(ISqlCommand<T> command)
        {
            var dummy = new DummyCommandAsync(new SqlCommand());
            new SqlCommandStrategy(_interactionFactory).SetupCommand(command, dummy);
            return _stringifier.Stringify(dummy);
        }

        public string Stringify<T>(ISqlQuery<T> query)
        {
            var dummy = new DummyCommandAsync(new SqlCommand());
            new SqlQueryStrategy(_interactionFactory).SetupCommand(query, dummy);
            return _stringifier.Stringify(dummy);
        }

        public string Stringify(ISqlCommandSimple command)
        {
            var dummy = new DummyCommandAsync(new SqlCommand());
            new SqlCommandSimpleStrategy().SetupCommand(command, dummy);
            return _stringifier.Stringify(dummy);
        }

        private sealed class DummyCommandAsync : IDbCommandAsync
        {
            public DummyCommandAsync(IDbCommand command)
            {
                Command = command;
            }

            public IDbCommand Command { get; }

            public void Dispose() => throw new System.NotImplementedException();


            public IDataReaderAsync ExecuteReader() => throw new System.NotImplementedException();

            public Task<IDataReaderAsync> ExecuteReaderAsync(CancellationToken cancellationToken) => throw new System.NotImplementedException();

            public Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) => throw new System.NotImplementedException();
        }
    }
}