using System.Data.SqlClient;
using CastIron.Sql.Execution;

namespace CastIron.Sql
{
    /// <summary>
    /// Facilitates pretty-printing of commands for the purposes of debugging, logging and auditing
    /// Notice that the stringified commands may not be complete or valid SQL in some situations
    /// </summary>
    public class QueryObjectStringifier
    {
        private readonly IDataInteractionFactory _interactionFactory;
        private readonly DbCommandStringifier _stringifier;

        public QueryObjectStringifier(IDataInteractionFactory interactionFactory)
        {
            _interactionFactory = interactionFactory;
            _stringifier = DbCommandStringifier.GetDefaultInstance();
        }

        public string Stringify<T>(ISqlQuerySimple<T> query)
        {
            var dummy = new SqlCommand();
            new SqlQuerySimpleStrategy().SetupCommand(query, dummy);
            return _stringifier.Stringify(dummy);
        }

        public string Stringify<T>(ISqlConnectionAccessor<T> query)
        {
            return "";
        }

        public string Stringify(ISqlCommand command)
        {
            var dummy = new SqlCommand();
            new SqlCommandStrategy(_interactionFactory).SetupCommand(command, dummy);
            return _stringifier.Stringify(dummy);
        }

        public string Stringify<T>(ISqlCommand<T> command)
        {
            var dummy = new SqlCommand();
            new SqlCommandStrategy(_interactionFactory).SetupCommand(command, dummy);
            return _stringifier.Stringify(dummy);
        }

        public string Stringify<T>(ISqlQuery<T> query)
        {
            var dummy = new SqlCommand();
            new SqlQueryStrategy(_interactionFactory).SetupCommand(query, dummy);
            return _stringifier.Stringify(dummy);
        }

        public string Stringify(ISqlCommandSimple command)
        {
            var dummy = new SqlCommand();
            new SqlCommandSimpleStrategy().SetupCommand(command, dummy);
            return _stringifier.Stringify(dummy);
        }
    }
}