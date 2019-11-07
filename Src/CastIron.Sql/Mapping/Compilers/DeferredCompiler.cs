using System;

namespace CastIron.Sql.Mapping.Compilers
{
    public class DeferredCompiler : ICompiler
    {
        private readonly Func<ICompiler> _getCompiler;

        public DeferredCompiler(Func<ICompiler> getCompiler)
        {
            _getCompiler = getCompiler;
        }

        public ConstructedValueExpression Compile(MapState state) => _getCompiler().Compile(state);
    }
}
