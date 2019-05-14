using System;
using System.Data;
using CastIron.Sql.Execution;

namespace CastIron.Sql
{
    /// <summary>
    /// Exception wrapper type which encapsulates an exception and (if available) the SQL command
    /// text and parameter information which was attempting to execute
    /// </summary>
    public class SqlQueryException : Exception
    {
        public SqlQueryException()
        {
        }

        public SqlQueryException(string message)
            : base(message)
        {
        }

        public SqlQueryException(string message, Exception inner)
            : base(message, inner)
        {
        }

        public SqlQueryException(string message, string sql, Exception inner)
            : this(message + "\n\n" + (sql ?? ""), inner)
        {
        }

        /// <summary>
        /// Wrap an Exception and IDbCommand into a new exception which contains helpful details
        /// </summary>
        /// <param name="e"></param>
        /// <param name="command"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static SqlQueryException Wrap(Exception e, IDbCommand command, int index = -1)
        {
            var sql = DbCommandStringifier.GetDefaultInstance().Stringify(command);
            var message = e.Message;
            if (index >= 0)
                message = $"Error executing statement {index}\n{e.Message}";
            return new SqlQueryException(message, sql, e);
        }
    }
}