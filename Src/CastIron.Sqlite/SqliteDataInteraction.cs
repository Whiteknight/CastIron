using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using CastIron.Sql;
using CastIron.Sql.Utility;

namespace CastIron.Sqlite
{
    public class SqliteDataInteraction : IDataInteraction
    {
        public SqliteDataInteraction(IDbCommand command)
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
            param.Direction = ParameterDirection.Input;
            param.ParameterName = NormalizeParameterName(name);
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

        public IDataInteraction AddParametersWithValues(object parameters)
        {
            Assert.ArgumentNotNull(parameters, nameof(parameters));

            var properties = parameters.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).ToDictionary(p => p.Name, p => p.GetValue(parameters));
            return AddParametersWithValues(properties);
        }

        public IDataInteraction AddOutputParameter(string name, DbType dbType, int size)
        {
            throw new Exception("SQLite does not support OUTPUT or INPUT/OUTPUT parameters.");
        }

        public IDataInteraction AddInputOutputParameter(string name, object value, DbType dbType, int size)
        {
            throw new Exception("SQLite does not support OUTPUT or INPUT/OUTPUT parameters.");
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

        private static string NormalizeParameterName(string name)
        {
            return name.StartsWith("@") ? name : "@" + name;
        }
    }
}