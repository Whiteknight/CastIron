using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using CastIron.Sql.Execution;

namespace CastIron.Postgres
{
    /// <summary>
    /// Attempts best-effort stringification of a Postgres command, for debugging and auditing
    /// purposes
    /// </summary>
    public class PostgresDbCommandStringifier : IDbCommandStringifier
    {
        private static readonly HashSet<DbType> _quotedDbTypes = new HashSet<DbType>
        {
            DbType.String,
            DbType.StringFixedLength,
            DbType.AnsiStringFixedLength,
            DbType.AnsiString,
            DbType.Date,
            DbType.DateTime,
            DbType.DateTime2,
            DbType.Guid,
            DbType.DateTimeOffset,
            DbType.Xml
        };

        public string Stringify(IDbCommand command)
        {
            var sb = new StringBuilder();
            Stringify(command, sb);
            return sb.ToString();
        }

        public void Stringify(IDbCommand command, StringBuilder sb)
        {
            if (command == null)
                return;

            foreach (var t in command.Parameters)
            {
                if (!(t is SqlParameter param))
                    continue;
                sb.Append("--DECLARE ");
                sb.Append(param.ParameterName);
                sb.Append(" ");
                sb.Append(param.SqlDbType);
                if (param.Size > 0)
                {
                    sb.Append("(");
                    sb.Append(param.Size);
                    sb.Append(")");
                }

                if (param.Direction == ParameterDirection.Input || param.Direction == ParameterDirection.InputOutput)
                {
                    sb.Append(" = ");
                    var value = param.SqlValue;
                    if (_quotedDbTypes.Contains(param.DbType))
                        value = "'" + value.ToString().Replace("'", "''") + "'";
                    sb.Append(value);
                }

                sb.AppendLine(";");
            }

            switch (command.CommandType)
            {
                case CommandType.StoredProcedure:
                    sb.Append("EXECUTE ");
                    sb.Append(command.CommandText);
                    for (var i = 0; i < command.Parameters.Count; i++)
                    {
                        if (!(command.Parameters[i] is SqlParameter param))
                            continue;
                        sb.Append(param.ParameterName);
                        if (param.Direction == ParameterDirection.Output || param.Direction == ParameterDirection.InputOutput)
                            sb.Append(" OUTPUT");
                        sb.Append(i == command.Parameters.Count - 1 ? ";" : ", ");
                    }
                    break;
                case CommandType.Text:
                    sb.AppendLine(command.CommandText);
                    break;
                case CommandType.TableDirect:
                    // TODO: This
                    break;
            }
        }
    }
}