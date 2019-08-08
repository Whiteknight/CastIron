using System.Threading;
using CastIron.Sql.Utility;

namespace CastIron.Sql.Mapping
{
    public static class MapCompilation
    {
        private static CachingMappingCompiler _cachingInstance;
        private static MapCompiler _uncachedInstance;
        private static IMapCompiler _defaultInstance;

        public static CachingMappingCompiler GetCachingInstance()
        {
            if (_cachingInstance != null)
                return _cachingInstance;

            var uncached = GetMapCompilerInstance();
            var newInstance = new CachingMappingCompiler(uncached);
            var oldValue = Interlocked.CompareExchange(ref _cachingInstance, newInstance, null);
            return oldValue ?? newInstance;
        }

        public static MapCompiler GetMapCompilerInstance()
        {
            if (_uncachedInstance != null)
                return _uncachedInstance;

            var newInstance = new MapCompiler();
            var oldValue = Interlocked.CompareExchange(ref _uncachedInstance, newInstance, null);
            return oldValue ?? newInstance;
        }

        public static IMapCompiler GetDefaultInstance()
        {
            if (_defaultInstance != null)
                return _defaultInstance;

            var newInstance = GetCachingInstance();
            var oldValue = Interlocked.CompareExchange(ref _defaultInstance, newInstance, null);
            return oldValue ?? newInstance;
        }

        public static void SetDefaultInstance(IMapCompiler compiler)
        {
            Argument.NotNull(compiler, nameof(compiler));
            _defaultInstance = compiler;
        }
    }
}