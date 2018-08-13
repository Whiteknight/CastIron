using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using CastIron.Sql.Execution;

namespace CastIron.Sql.Mapping
{
    public class DataRecordMappingEnumerable<T> : IEnumerable<T>
    {
        private readonly IDataReader _reader;
        private readonly IExecutionContext _context;
        private readonly Func<IDataRecord, T> _map;
        private bool _alreadyRead;

        // TODO: Provide some standard maps: Map columnName->propertyName, or provide a map of columnNumber->propertyName to use
        public DataRecordMappingEnumerable(IDataReader reader, IExecutionContext context, Func<IDataRecord, T> map = null)
        {
            _reader = reader;
            _context = context;
            _map = map ?? new PropertyAndConstructorRecordMapperCompiler().CompileExpression<T>(reader);
            _alreadyRead = false;
        }

        public DataRecordMappingEnumerable(IDataReader reader, IExecutionContext context, IRecordMapperCompiler compiler)
        {
            _reader = reader;
            _context = context;
            _map = (compiler ?? new PropertyAndConstructorRecordMapperCompiler()).CompileExpression<T>(reader);
            _alreadyRead = false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (_alreadyRead)
                throw new Exception("Cannot read the same result set more than once. Please cache your results and read from the cache");
            if (_context.IsCompleted)
                throw new Exception("The connection is closed and the result set cannot be read");
            _alreadyRead = true;
            return new ResultSetEnumerator(_reader, _context, _map);
        }

        private class ResultSetEnumerator : IEnumerator<T>
        {
            private readonly IDataReader _reader;
            private readonly IExecutionContext _context;
            private readonly Func<IDataRecord, T> _read;

            public ResultSetEnumerator(IDataReader reader, IExecutionContext context, Func<IDataRecord, T> read)
            {
                _reader = reader;
                _context = context;
                _read = read;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (_context.IsCompleted)
                {
                    Current = default(T);
                    return false;
                }

                bool ok = _reader.Read();
                Current = ok ? _read(_reader) : default(T);
                return ok;
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }

            public T Current { get; private set; }

            object IEnumerator.Current => Current;
        }        
    }
}