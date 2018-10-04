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
            AdvanceToResultSet(_currentSet + 1);
            return new DataRecordMappingEnumerable<T>(_reader, _context, map);
        }

        public IEnumerable<T> GetNextEnumerable<T>(IRecordMapperCompiler compiler)
        {
            AdvanceToResultSet(_currentSet + 1);
            return new DataRecordMappingEnumerable<T>(_reader, _context, compiler);
        }

        public IEnumerable<T> GetNextEnumerable<T>(Func<ISubclassMapping<T>, ISubclassMapping<T>> setup, IRecordMapperCompiler compiler = null)
        {
            AdvanceToResultSet(_currentSet + 1);
            var mapping = new SubclassMapping<T>(compiler);
            setup(mapping);
            return new DataRecordMappingEnumerable<T>(_reader, _context, mapping.BuildThunk(_reader));
        }

        private void AdvanceToResultSet(int num)
        {
            // TODO: Review all this logic to make sure it is sane and necessary
            if (_currentSet > num)
                throw new Exception("Cannot read result sets out of order. At Set=" + _currentSet + " but requested Set=" + num);
            if (_currentSet == 0 && num == 1)
            {
                _currentSet = num;
                return;
            }

            while (_currentSet < num)
            {
                _currentSet++;
                if (!_reader.NextResult())
                    throw new Exception("Could not read result Set=" + _currentSet + " (requested result Set=" + num + ")");
            }

            if (_currentSet != num)
                throw new Exception("Could not find result Set=" + num);
        }
    }
}