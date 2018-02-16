using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace CastIron.Sql.Commands
{
    public class SqlStoredProcWithOutParametersCommand<T> : ISqlCommandRaw<T>
    {
        private readonly string _storedProcName;
        private readonly Dictionary<string, object> _parameters;
        private Func<SqlQueryResult, T> _onResult;

        public SqlStoredProcWithOutParametersCommand(string storedProcName)
        {
            _storedProcName = storedProcName;
            _parameters = new Dictionary<string, object>();
        }

        public T ReadOutputs(SqlQueryResult result)
        {
            if (_onResult != null)
                return _onResult(result);
            return default(T);
        }

        public bool SetupCommand(SqlCommand command)
        {
            if (string.IsNullOrEmpty(_storedProcName))
                return false;
            command.CommandText = _storedProcName;
            command.CommandType = CommandType.StoredProcedure;
            foreach (var p in _parameters)
            {
                var param = new SqlParameter(p.Key, p.Value); // cmd.CreateParameter();
                //orderIdParam.ParameterName = p.Key;
                //orderIdParam.Value = p.Value;
                //orderIdParam.Direction = ParameterDirection.Input;
                command.Parameters.Add(param);
            }
            return true;
        }

        public SqlStoredProcWithOutParametersCommand<T> AddParameter(string name, object value)
        {
            _parameters.Add(name, value);
            return this;
        }

        public SqlStoredProcWithOutParametersCommand<T> MapResult(Func<SqlQueryResult, T> onResult)
        {
            _onResult = onResult;
            return this;
        }
    }
}