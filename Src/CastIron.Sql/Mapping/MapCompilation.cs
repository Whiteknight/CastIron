using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CastIron.Sql.Mapping.ScalarCompilers;
using CastIron.Sql.Utility;

namespace CastIron.Sql.Mapping
{
    /// <summary>
    /// Accessor class to get pre-constructed instances of important compiler classes
    /// </summary>
    public static class MapCompilation
    {
        // TODO: Move all this stuff onto the SqlRunner, SqlRunnerCore and/or ExecutionContext
        private static CachingMapCompiler _cachingInstance;
        private static MapCompiler _uncachedInstance;
        private static IMapCompiler _defaultInstance;

        public static CachingMapCompiler GetCachingInstance()
        {
            if (_cachingInstance != null)
                return _cachingInstance;

            var uncached = GetMapCompilerInstance();
            var newInstance = new CachingMapCompiler(uncached);
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

        // TODO: Would like to be able to configure compilers at the ISqlRunner level and pass it down into
        // the map compilation machinery instead of having a static singleton list
        // TODO: If the column is a serialized type like XML or JSON, we should be able to deserialize that into an object.
        private static readonly IReadOnlyList<IScalarMapCompiler> _defaultScalarMapCompilers = new List<IScalarMapCompiler>
        {
            new SameTypeMapCompiler(),
            new ObjectMapCompiler(),
            new ToStringMapCompiler(),
            new NumericConversionMapCompiler(),
            new NumberToBoolMapCompiler(),
            new StringToGuidMapCompiler(),
            new ConvertibleMapCompiler(),
            new DefaultScalarMapCompiler(),
        };

        public static IEnumerable<IScalarMapCompiler> GetDefaultScalarMapCompilers()
            => _defaultScalarMapCompilers;
    }
}