using System.Collections.Generic;
using System.Threading;
using CastIron.Sql.Mapping.ScalarCompilers;

namespace CastIron.Sql.Mapping
{
    public class MapCompilerSource : IMapCompilerSource
    {
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

        private readonly List<IScalarMapCompiler> _scalarMapCompilers;

        private IMapCompiler _defaultCompiler;

        public MapCompilerSource()
        {
            _scalarMapCompilers = new List<IScalarMapCompiler>(_defaultScalarMapCompilers);
            _defaultCompiler = null;
        }

        public IMapCompiler GetCompiler()
        {
            var defaultCompiler = _defaultCompiler;
            if (defaultCompiler == null)
            {
                defaultCompiler = GetCompilerInternal();
                Interlocked.CompareExchange(ref _defaultCompiler, defaultCompiler, null);
            }
            return _defaultCompiler ?? defaultCompiler;
        }

        private IMapCompiler GetCompilerInternal()
        {
            if (_scalarMapCompilers.Count == 0)
                throw MapCompilerException.NoScalarMapCompilers();

            return new MapCompiler(_scalarMapCompilers);
        }

        public void Add(IScalarMapCompiler compiler)
        {
            _defaultCompiler = null;
            _scalarMapCompilers.Add(compiler);
        }

        public void Clear()
        {
            if (_scalarMapCompilers.Count > 0)
            {
                _defaultCompiler = null;
                _scalarMapCompilers.Clear();
            }
        }
    }
}