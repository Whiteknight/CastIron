using System;
using System.Data;
using System.Text;

namespace CastIron.Sql.Execution
{
    public static class ExceptionExtensions
    {
        public static SqlProblemException WrapAsSqlProblemException(this Exception e, IDbCommand command, int index = -1)
        {
            var sb = new StringBuilder();
            new DbCommandStringifier().Stringify(command, sb);

            var message = e.Message;
            if (index >= 0)
                message = $"Error executing statement {index}\n{e.Message}";
            return new SqlProblemException(message, sb.ToString(), e);
        }
    }
}