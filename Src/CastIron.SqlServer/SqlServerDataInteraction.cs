using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using CastIron.Sql;
using CastIron.Sql.Utility;

namespace CastIron.SqlServer
{
    public class SqlServerDataInteraction : IDataInteraction
    {
        public SqlServerDataInteraction(IDbCommand command)
        {
            Argument.NotNull(command, nameof(command));
            Command = command;
        }

        public IDbCommand Command { get; }

        public bool IsValid => !string.IsNullOrEmpty(Command?.CommandText);

        public IDataInteraction AddParameter(Action<IDbDataParameter> setup)
        {
            Argument.NotNull(setup, nameof(setup));
            var param = Command.CreateParameter();
            setup.Invoke(param);
            param.ParameterName = NormalizeParameterName(param.ParameterName);
            Command.Parameters.Add(param);
            return this;
        }

        public IDataInteraction AddParameterWithValue(string name, object value)
        {
            Argument.NotNullOrEmpty(name, nameof(name));

            return AddParameter(param =>
            {
                param.Direction = ParameterDirection.Input;
                param.ParameterName = NormalizeParameterName(name);
                param.Value = value;
            });
        }

        public IDataInteraction AddParametersWithValues(IEnumerable<KeyValuePair<string, object>> parameters)
        {
            Argument.NotNull(parameters, nameof(parameters));

            foreach (var parameter in parameters)
                AddParameterWithValue(parameter.Key, parameter.Value);
            return this;
        }

        public IDataInteraction AddParametersWithValues(object parameters)
        {
            Argument.NotNull(parameters, nameof(parameters));

            var properties = parameters.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .ToDictionary(p => p.Name, p => p.GetValue(parameters));
            return AddParametersWithValues(properties);
        }

        public IDataInteraction AddOutputParameter(string name, DbType dbType, int size)
        {
            Argument.NotNullOrEmpty(name, nameof(name));
            return AddParameter(param =>
            {
                param.Direction = ParameterDirection.Output;
                param.ParameterName = NormalizeParameterName(name);
                param.DbType = dbType;
                if (size > 0)
                    param.Size = size;
            });
        }

        public IDataInteraction AddInputOutputParameter(string name, object value, DbType dbType, int size)
        {
            Argument.NotNullOrEmpty(name, nameof(name));
            return AddParameter(param =>
            {
                param.Direction = ParameterDirection.InputOutput;
                param.ParameterName = NormalizeParameterName(name);
                param.DbType = dbType;
                if (size > 0)
                    param.Size = size;
                param.Value = value;
            });
        }

        public IDataInteraction ExecuteText(string sqlText)
        {
            Argument.NotNullOrEmpty(sqlText, nameof(sqlText));

            Command.CommandType = CommandType.Text;
            Command.CommandText = sqlText;
            return this;
        }

        public IDataInteraction CallStoredProc(string procedureName)
        {
            Argument.NotNullOrEmpty(procedureName, nameof(procedureName));

            Command.CommandType = CommandType.StoredProcedure;
            Command.CommandText = procedureName;
            return this;
        }

        private static string NormalizeParameterName(string name)
            => name.StartsWith("@") ? name : "@" + name;
    }
}
