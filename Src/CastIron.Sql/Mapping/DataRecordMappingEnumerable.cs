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

        public DataRecordMappingEnumerable(IDataReader reader, Func<IDataRecord, T> map = null)
        {
            _reader = reader;
            _map = map ?? CreateDefaultMap(reader);
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

        private static Func<IDataRecord, T> CreateDefaultMap(IDataReader reader)
        {
            if (reader == null)
                return r => default(T);

            var columnNames = new Dictionary<string, int>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var name = reader.GetName(i).ToLowerInvariant();
                if (string.IsNullOrEmpty(name))
                    continue;
                if (columnNames.ContainsKey(name))
                    continue;
                columnNames.Add(name, i);
            }

            return DataRecordMapperCompiler.CompileExpression<T>(columnNames);
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