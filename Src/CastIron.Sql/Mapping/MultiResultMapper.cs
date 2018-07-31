using System;
using System.Collections.Generic;
using System.Data;
using CastIron.Sql.Execution;

namespace CastIron.Sql.Mapping
{
    public class MultiResultMapper
    {
        private readonly IDataReader _reader;
        private readonly IExecutionContext _context;
        private int _currentSet;

        public MultiResultMapper(IDataReader reader, IExecutionContext context)
        {
            _reader = reader;
            _context = context;
            _currentSet = 0;
        }

        public bool SkipNext()
        {
            _currentSet++;
            return _reader.NextResult();
        }

        public IEnumerable<T> GetNextEnumerable<T>(Func<IDataRecord, T> map = null)
        {
            return GetResultSetAsEnumerable(_currentSet + 1, map);
        }

        private IEnumerable<T> GetResultSetAsEnumerable<T>(int num, Func<IDataRecord, T> map = null)
        {
            // TODO: Review all this logic to make sure it is sane and necessary
            if (_currentSet > num)
                throw new Exception("Cannot read result sets out of order. At Set=" + _currentSet + " but requested Set=" + num);
            if (_currentSet == 0 && num == 1)
            {
                _currentSet = num;
                map = map ?? DefaultMappings.Create<T>(_reader);
                return new DataRecordMappingEnumerable<T>(_reader, _context, map);
            }

            while (_currentSet < num)
            {
                _currentSet++;
                if (!_reader.NextResult())
                    throw new Exception("Could not read result Set=" + _currentSet + " (requested result Set=" + num + ")");
            }

            if (_currentSet != num)
                throw new Exception("Could not find result Set=" + num);

            map = map ?? DefaultMappings.Create<T>(_reader);
            return new DataRecordMappingEnumerable<T>(_reader, _context, map);
        }
    }
}