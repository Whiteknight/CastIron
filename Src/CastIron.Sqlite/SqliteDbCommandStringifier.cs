using System.Collections.Generic;
using System.Data;
using System.Text;
using CastIron.Sql;
using CastIron.Sql.Execution;
using Microsoft.Data.Sqlite;

namespace CastIron.Sqlite
{
    /// <summary>
    /// Attempts best-effort stringification of a Sqlite command, for debugging
    /// and auditing purposes
    /// </summary>
    public class SqliteDbCommandStringifier : IDbCommandStringifier
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

        public string Stringify(IDbCommandAsync command)
        {
            return Stringify(command.Command);
        }

        public void Stringify(IDbCommand command, StringBuilder sb)
        {
            if (command == null)
                return;

            foreach (var t in command.Parameters)
                StringifyParameter(t, sb);

            switch (command.CommandType)
            {
                case CommandType.StoredProcedure:
                    StringifyStoredProc(command, sb);
                    break;
                case CommandType.Text:
                    sb.AppendLine(command.CommandText);
                    break;
                case CommandType.TableDirect:
                    // This won't happen often so we aren't worrying about it
                    break;
            }
        }

        private void StringifyParameter(object t, StringBuilder sb)
        {
            if (!(t is SqliteParameter param))
                return;
            sb.Append("--DECLARE ");
            sb.Append(param.ParameterName);
            sb.Append(" ");
            sb.Append(param.DbType);
            if (param.Size > 0)
            {
                sb.Append("(");
                sb.Append(param.Size);
                sb.Append(")");
            }

            if (param.Direction == ParameterDirection.Input || param.Direction == ParameterDirection.InputOutput)
            {
                sb.Append(" = ");
                var value = param.Value;
                if (_quotedDbTypes.Contains(param.DbType))
                    value = "'" + value.ToString().Replace("'", "''") + "'";
                sb.Append(value);
            }

            sb.AppendLine(";");
        }

        private void StringifyStoredProc(IDbCommand command, StringBuilder sb)
        {
            sb.Append("EXECUTE ");
            sb.Append(command.CommandText);
            for (var i = 0; i < command.Parameters.Count; i++)
            {
                if (!(command.Parameters[i] is SqliteParameter param))
                    continue;
                sb.Append(param.ParameterName);
                if (param.Direction == ParameterDirection.Output || param.Direction == ParameterDirection.InputOutput)
                    sb.Append(" OUTPUT");
                sb.Append(i == command.Parameters.Count - 1 ? ";" : ", ");
            }
        }

    }
}