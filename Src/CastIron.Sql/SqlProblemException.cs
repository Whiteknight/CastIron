using System;

namespace CastIron.Sql
{
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