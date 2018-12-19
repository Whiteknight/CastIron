using System;
using System.Data;
using CastIron.Sql.Execution;

namespace CastIron.Sql
{
    /// <summary>
    /// Exception wrapper type which encapsulates an exception and (if available) the SQL command
    /// which was attempting to execute
    /// </summary>
    public class SqlProblemException : Exception
    {
        public SqlProblemException()
        {
        }

        public SqlProblemException(string message)
            : base(message)
        {
        }

        public SqlProblemException(string message, Exception inner)
            : base(message, inner)
        {
        }

        public SqlProblemException(string message, string sql, Exception inner)
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
        public static SqlProblemException Wrap(Exception e, IDbCommand command, int index = -1)
        {
            var sql = DbCommandStringifier.GetDefaultInstance().Stringify(command);
            var message = e.Message;
            if (index >= 0)
                message = $"Error executing statement {index}\n{e.Message}";
            return new SqlProblemException(message, sql, e);
        }
    }
}