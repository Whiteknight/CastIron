using System;
using System.Collections.Generic;
using System.Data;

namespace CastIron.Sql.Mapping
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

        public void MapOntoRaw<TKey, TRecord, TAggregate>(IReadOnlyDictionary<TKey, TAggregate> objects, Func<IDataRecord, TKey> getKey, Func<IDataRecord, TRecord> map,  Action<TRecord, TAggregate> mapOnto)
        {
            Assert.ArgumentNotNull(getKey, nameof(getKey));
            Assert.ArgumentNotNull(objects, nameof(objects));
            Assert.ArgumentNotNull(mapOnto, nameof(mapOnto));

            map = map ?? DefaultMappings.Create<TRecord>(_reader);

            while (_reader.Read())
            {
                var key = getKey(_reader);
                if (!objects.ContainsKey(key))
                    continue;
                var aggregate = objects[key];
                var record = map(_reader);
                mapOnto(record, aggregate);
            }
        }

        public void MapOntoRaw<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> objects, Func<IDataRecord, TKey> getKey, Action<IDataRecord, TValue> mapOnto)
        {
            Assert.ArgumentNotNull(getKey, nameof(getKey));
            Assert.ArgumentNotNull(objects, nameof(objects));
            Assert.ArgumentNotNull(mapOnto, nameof(mapOnto));

            while (_reader.Read())
            {
                var key = getKey(_reader);
                if (!objects.ContainsKey(key))
                    continue;
                var aggregate = objects[key];
                mapOnto(_reader, aggregate);
            }
        }

        public void MapNextOntoRaw<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> objects, Func<IDataRecord, TKey> getKey, Action<IDataRecord, TValue> mapOnto)
        {
            NextResultSet();
            MapOntoRaw(objects, getKey, mapOnto);
        }

        public void MapOnto<TKey, TRecord, TAggregate>(IReadOnlyDictionary<TKey, TAggregate> objects, Func<IDataRecord, TRecord> map, Func<TRecord, TKey> getKey, Action<TRecord, TAggregate> mapOnto)
        {
            Assert.ArgumentNotNull(getKey, nameof(getKey));
            Assert.ArgumentNotNull(objects, nameof(objects));
            Assert.ArgumentNotNull(mapOnto, nameof(mapOnto));

            map = map ?? DefaultMappings.Create<TRecord>(_reader);

            while (_reader.Read())
            {
                var record = map(_reader);
                var key = getKey(record);
                if (!objects.ContainsKey(key))
                    continue;
                var aggregate = objects[key];

                mapOnto(record, aggregate);
            }
        }

        public void MapNextOnto<TKey, TRecord, TAggregate>(IReadOnlyDictionary<TKey, TAggregate> objects, Func<IDataRecord, TRecord> map, Func<TRecord, TKey> getKey, Action<TRecord, TAggregate> mapOnto)
        {
            NextResultSet();
            MapOnto(objects, map, getKey, mapOnto);
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

            map = map ?? DefaultMappings.Create<T>(_reader);
            return new DataRecordMappingEnumerable<T>(_reader, map);
        }
    }
}