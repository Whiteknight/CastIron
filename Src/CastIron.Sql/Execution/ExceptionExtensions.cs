using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace CastIron.Sql.Execution
{
    public static class ExceptionExtensions
    {
        public static SqlProblemException WrapAsSqlProblemException(this Exception e, IDbCommand command, string text, int index = -1)
        {
            var sb = new StringBuilder();
            if (command != null)
            {
                for (int i = 0; i < command.Parameters.Count; i++)
                {
                    if (!(command.Parameters[i] is SqlParameter param))
                        continue;
                    sb.Append("--DECLARE ");
                    sb.Append(param.ParameterName);
                    sb.Append(" ");
                    sb.Append(param.DbType);
                    sb.Append(" = ");
                    sb.Append(param.SqlValue);
                    sb.AppendLine(";");
                }
                sb.AppendLine();
            }

            if (!string.IsNullOrEmpty(text))
                sb.AppendLine(text);

            var message = e.Message;
            if (index >= 0)
                message = $"Error executing statement {index}\n{e.Message}";
            return new SqlProblemException(message, sb.ToString(), e);
        }
    }
}