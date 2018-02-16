using System;
using System.Collections.Generic;
using System.Data;

namespace CastIron.Sql
{
    public class MultiResultMapper
    {
        private readonly IDataReader _reader;
        private int _currentSet;

        public MultiResultMapper(IDataReader reader)
        {
            _reader = reader;
            _currentSet = 1;
        }

        public IEnumerable<T> AsEnumerable<T>(Func<IDataRecord, T> map = null)
        {
            return GetResultSetAsEnumerable(_currentSet, map);
        }

        public bool NextResultSet()
        {
            _currentSet++;
            return _reader.NextResult();
        }

        public IEnumerable<T> NextResultSetAsEnumerable<T>(Func<IDataRecord, T> map = null)
        {
            return GetResultSetAsEnumerable(_currentSet + 1, map);
        }

        private IEnumerable<T> GetResultSetAsEnumerable<T>(int num, Func<IDataRecord, T> map = null)
        {
            if (_currentSet > num)
                throw new Exception("Cannot read result sets out of order. At Set=" + _currentSet + " but requested Set=" + num);
            while (_currentSet < num)
            {
                _currentSet++;
                if (!_reader.NextResult())
                    throw new Exception("Could not read result Set=" + _currentSet + " (requested result Set=" + num + ")");
            }
            if (_currentSet != num)
                throw new Exception("Could not find result Set=" + num);
            return new DataRecordMappingEnumerable<T>(_reader, map);
        }
    }
}