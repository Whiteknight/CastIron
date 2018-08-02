﻿using System.Data.SqlClient;
using CastIron.Sql.Execution;

namespace CastIron.Sql
{
    /// <summary>
    /// Facilitates pretty-printing of commands for the purposes of debugging, logging and auditing
    /// </summary>
    public class QueryObjectStringifier
    {
        private readonly DbCommandStringifier _stringifier;

        public QueryObjectStringifier()
        {
            _stringifier = new DbCommandStringifier();
        }

        public string Stringify<T>(ISqlQuery<T> query)
        {
            var dummy = new SqlCommand();
            new SqlQueryStrategy<T>(query).SetupCommand(dummy);
            return _stringifier.Stringify(dummy);
        }

        public string Stringify<T>(ISqlQueryRawConnection<T> query)
        {
            return "";
        }

        public string Stringify(ISqlCommandRawCommand command)
        {
            var dummy = new SqlCommand();
            new SqlCommandRawStrategy(command).SetupCommand(dummy);
            return _stringifier.Stringify(dummy);
        }

        public string Stringify<T>(ISqlCommandRawCommand<T> command)
        {
            var dummy = new SqlCommand();
            new SqlCommandRawStrategy<T>(command).SetupCommand(dummy);
            return _stringifier.Stringify(dummy);
        }

        public string Stringify<T>(ISqlQueryRawCommand<T> query)
        {
            var dummy = new SqlCommand();
            new SqlQueryRawCommandStrategy<T>(query).SetupCommand(dummy);
            return _stringifier.Stringify(dummy);
        }

        public string Stringify(ISqlCommand command)
        {
            var dummy = new SqlCommand();
            new SqlCommandStrategy(command).SetupCommand(dummy);
            return _stringifier.Stringify(dummy);
        }
    }
}