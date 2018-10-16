using System.Collections.Generic;
using System.Data;

namespace CastIron.Sql.SqlServer
{
    public class SqlServerDataInteraction : IDataInteraction
    {
        public SqlServerDataInteraction(IDbCommand command)
        {
            Assert.ArgumentNotNull(command, nameof(command));
            Command = command;
        }

        public IDbCommand Command { get; }

        public bool IsValid => !string.IsNullOrEmpty(Command?.CommandText);

        public IDataInteraction AddParameterWithValue(string name, object value)
        {
            Assert.ArgumentNotNullOrEmpty(name, nameof(name));

            var param = Command.CreateParameter();
            param.ParameterName = name.StartsWith("@") ? name : "@" + name;
            param.Value = value;
            Command.Parameters.Add(param);
            return this;
        }

        public IDataInteraction AddParametersWithValues(IEnumerable<KeyValuePair<string, object>> parameters)
        {
            Assert.ArgumentNotNull(parameters, nameof(parameters));

            foreach (var parameter in parameters)
                AddParameterWithValue(parameter.Key, parameter.Value);
            return this;
        }

        public IDataInteraction ExecuteText(string sqlText)
        {
            Assert.ArgumentNotNullOrEmpty(sqlText, nameof(sqlText));

            Command.CommandType = CommandType.Text;
            Command.CommandText = sqlText;
            return this;
        }

        public IDataInteraction CallStoredProc(string procedureName)
        {
            Assert.ArgumentNotNullOrEmpty(procedureName, nameof(procedureName));

            Command.CommandType = CommandType.StoredProcedure;
            Command.CommandText = procedureName;
            return this;
        }
    }
}