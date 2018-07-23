using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;

namespace CastIron.Sql.Mapping
{
    public class DataRecordMappingEnumerable<T> : IEnumerable<T>
    {
        private readonly IDataReader _reader;
        private readonly Func<IDataRecord, T> _map;
        private bool _alreadyRead;

        // TODO: Provide some standard maps: Map columnName->propertyName, or provide a map of columnNumber->propertyName to use
        public DataRecordMappingEnumerable(IDataReader reader, Func<IDataRecord, T> map = null)
        {
            _reader = reader;
            _map = map;
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
            _alreadyRead = true;
            return new ResultSetEnumerator(_reader, _map);
        }

        private class ResultSetEnumerator : IEnumerator<T>
        {
            private readonly IDataReader _reader;
            private readonly Func<IDataRecord, T> _read;

            public ResultSetEnumerator(IDataReader reader, Func<IDataRecord, T> read)
            {
                _reader = reader;
                _read = read;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                bool ok = _reader.Read();
                if (ok)
                    Current = _read(_reader);
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