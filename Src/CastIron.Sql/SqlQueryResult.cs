﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace CastIron.Sql
{
    public class SqlQueryResult
    {
        private readonly SqlCommand _command;
        private readonly SqlDataReader _reader;
        private bool _isConsumed;

        public SqlQueryResult(SqlCommand command, SqlDataReader reader)
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
            return new DataRecordMappingEnumerable<T>(_reader, map);
        }

        public object GetOutputParameter(string name)
        {
            if (!_command.Parameters.Contains(name))
                return null;
            var param = _command.Parameters[name];
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