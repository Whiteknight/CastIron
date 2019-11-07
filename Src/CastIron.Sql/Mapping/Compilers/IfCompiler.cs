using System;

namespace CastIron.Sql.Mapping.Compilers
{
    /// <summary>
    /// Executes the inner compiler if a predicate is satisfied
    /// </summary>
    public class IfCompiler : ICompiler
    {
        private readonly Func<MapState, bool> _predicate;
        private readonly ICompiler _compiler;

        public IfCompiler(Func<MapState, bool> predicate, ICompiler compiler)
        {
            _predicate = predicate;
            _compiler = compiler;
        }

        public ConstructedValueExpression Compile(MapState state)
        {
            if (_predicate(state))
                return _compiler.Compile(state);
            return ConstructedValueExpression.Nothing;
        }
    }
}