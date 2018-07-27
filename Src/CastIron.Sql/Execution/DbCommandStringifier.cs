using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace CastIron.Sql.Execution
{
    public class DbCommandStringifier
    {
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

            for (int i = 0; i < command.Parameters.Count; i++)
            {
                if (!(command.Parameters[i] is SqlParameter param))
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
                    sb.Append(param.SqlValue);
                }

                sb.AppendLine(";");
            }

            switch (command.CommandType)
            {
                case CommandType.StoredProcedure:
                    sb.Append("EXECUTE ");
                    sb.Append(command.CommandText);
                    for (int i = 0; i < command.Parameters.Count; i++)
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