using System;

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
    }
}