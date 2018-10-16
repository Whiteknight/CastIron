using System.Data.SqlClient;
using CastIron.Sql.Execution;

namespace CastIron.Sql
{
    /// <summary>
    /// Facilitates pretty-printing of commands for the purposes of debugging, logging and auditing
    /// </summary>
    public class QueryObjectStringifier
    {
        private readonly IDataInteractionFactory _interactionFactory;
        private readonly DbCommandStringifier _stringifier;

        public QueryObjectStringifier(IDataInteractionFactory interactionFactory)
        {
            _interactionFactory = interactionFactory;
            _stringifier = new DbCommandStringifier();
        }

        public string Stringify<T>(ISqlQuerySimple<T> query)
        {
            var dummy = new SqlCommand();
            new SqlQuerySimpleStrategy<T>(query).SetupCommand(dummy);
            return _stringifier.Stringify(dummy);
        }

        public string Stringify<T>(ISqlConnectionAccessor<T> query)
        {
            return "";
        }

        public string Stringify(ISqlCommand command)
        {
            var dummy = new SqlCommand();
            new SqlCommandRawStrategy(command, _interactionFactory).SetupCommand(dummy);
            return _stringifier.Stringify(dummy);
        }

        public string Stringify<T>(ISqlCommand<T> command)
        {
            var dummy = new SqlCommand();
            new SqlCommandRawStrategy<T>(command, _interactionFactory).SetupCommand(dummy);
            return _stringifier.Stringify(dummy);
        }

        public string Stringify<T>(ISqlQuery<T> query)
        {
            var dummy = new SqlCommand();
            new SqlQueryStrategy<T>(query, _interactionFactory).SetupCommand(dummy);
            return _stringifier.Stringify(dummy);
        }

        public string Stringify(ISqlCommandSimple command)
        {
            var dummy = new SqlCommand();
            new SqlCommandStrategy(command).SetupCommand(dummy);
            return _stringifier.Stringify(dummy);
        }
    }
}