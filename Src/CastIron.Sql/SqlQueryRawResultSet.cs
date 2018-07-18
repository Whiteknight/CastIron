using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace CastIron.Sql
{
    public class SqlQueryRawResultSet
    {
        private readonly IDbCommand _command;
        private readonly IDataReader _reader;
        private bool _isConsumed;

        public SqlQueryRawResultSet(IDbCommand command, IDataReader reader)
        {
            _command = command;
            _reader = reader;
        }

        public IDataReader AsRawReader()
        {
            AssertHasReader();
            MarkConsumed();
            return _reader;
        }

        public MultiResultMapper AsResultMapper()
        {
            AssertHasReader();
            MarkConsumed();
            return new MultiResultMapper(_reader);
        }

        public IEnumerable<T> AsEnumerable<T>(Func<IDataRecord, T> map = null)
        {
            if (_reader == null)
                throw new InvalidOperationException("Cannot map results to enumerable because the reader is null. Are you executing an ISqlCommand variant?");
            return new DataRecordMappingEnumerable<T>(_reader, map);
        }

        public object GetOutputParameter(string name)
        {
            if (!_command.Parameters.Contains(name))
                return null;

            if (!(_command.Parameters[name] is DbParameter param))
                return null;

            if (param.Direction == ParameterDirection.Input)
                return null;
            if (param.Value == DBNull.Value)
                return null;
            return param.Value;
        }

        public T GetOutputParameter<T>(string name)
        {
            var value = GetOutputParameter(name);
            if (value == null)
                return default(T);
            if (!(value is T))
                throw new Exception($"Cannot get value '{name}'. Expected type {typeof(T).FullName} but found {value.GetType().FullName}");
            return (T) value;
        }

        private void MarkConsumed()
        {
            if (_isConsumed)
                throw new Exception("SqlDataReader is forward-only, and cannot be consumed more than once");
            _isConsumed = true;
        }

        private void AssertHasReader()
        {
            if (_reader == null)
                throw new Exception("This result does not contain a data reader");
        }
    }
}