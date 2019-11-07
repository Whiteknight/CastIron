using System;

namespace CastIron.Sql.Mapping.Compilers
{
    /// <summary>
    /// Obtain a reference to a compiler at runtime to avoid circular references
    /// </summary>
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
