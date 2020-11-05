using System.Collections.Concurrent;
using System.Threading;

namespace CastIron.Sql.Mapping
{
    public class MapCache : IMapCache
    {
        private static IMapCache _defaultInstance;
        private readonly ConcurrentDictionary<object, ConcurrentDictionary<int, object>> _cache;

        public MapCache()
        {
            _cache = new ConcurrentDictionary<object, ConcurrentDictionary<int, object>>();
        }

        public static IMapCache GetDefaultInstance()
        {
            var thisValue = Interlocked.CompareExchange(ref _defaultInstance, null, null);
            if (thisValue != null)
                return thisValue;

            thisValue = new MapCache();
            var originalValue = Interlocked.CompareExchange(ref _defaultInstance, thisValue, null);
            return originalValue ?? thisValue;
        }

        public bool Cache(object key, int set, object map)
        {
            if (key == null || map == null)
                return false;
            var sets = _cache.GetOrAdd(key, _ => new ConcurrentDictionary<int, object>());
            return sets.TryAdd(set, map);
        }

        public void Clear()
        {
            _cache.Clear();
        }

        public void Remove(object key)
        {
            _ = _cache.TryRemove(key, out _);
        }

        public void Remove(object key, int set)
        {
            var ok = _cache.TryGetValue(key, out var sets);
            if (!ok)
                return;
            _ = sets.TryRemove(set, out _);
        }

        public object Get(object key, int set)
        {
            if (key == null)
                return null;
            var ok = _cache.TryGetValue(key, out var sets);
            if (!ok || sets == null)
                return null;
            return sets.TryGetValue(set, out var value) ? value : null;
        }
    }
}
