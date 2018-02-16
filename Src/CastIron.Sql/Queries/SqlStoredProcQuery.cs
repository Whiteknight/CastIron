using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace CastIron.Sql.Queries
{
    public class SqlStoredProcQuery<TResult> : ISqlQueryRaw<TResult>
    {
        private readonly string _storedProcName;
        private readonly Dictionary<string, object> _parameters;
        private Func<SqlQueryResult, TResult> _onReader;

        public SqlStoredProcQuery(string storedProcName)
        {
            _storedProcName = storedProcName;
            _parameters = new Dictionary<string, object>();
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

        public TResult Read(SqlQueryResult result)
        {
            if (_onReader == null)
                return default(TResult);
            return _onReader(result);
        }

        public SqlStoredProcQuery<TResult> AddParameter(string name, object value)
        {
            _parameters.Add(name, value);
            return this;
        }

        public SqlStoredProcQuery<TResult> MapResult(Func<SqlQueryResult, TResult> onReader)
        {
            _onReader = onReader;
            return this;
        }
    }
}
