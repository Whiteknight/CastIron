using System;
using System.Runtime.Serialization;

namespace CastIron.Sql
{
    /// <summary>
    /// Exception wrapper type which encapsulates an exception and (if available) the SQL command
    /// text and parameter information which was attempting to execute
    /// </summary>
    [Serializable]
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
        /// <param name="sql"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static SqlQueryException Wrap(Exception e, string sql, int index = -1)
        {
            var message = e.Message;
            if (index >= 0)
                message = $"Error executing statement {index}\n{e.Message}";
            return new SqlQueryException(message, sql, e);
        }

        protected SqlQueryException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}