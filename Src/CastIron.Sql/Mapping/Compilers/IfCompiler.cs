using System;

namespace CastIron.Sql.Mapping.Compilers
{
    /// <summary>
    /// Executes the inner compiler if a predicate is satisfied
    /// </summary>
    public class IfCompiler : ICompiler
    {
        private readonly Func<MapContext, bool> _predicate;
        private readonly ICompiler _compiler;

        public IfCompiler(Func<MapContext, bool> predicate, ICompiler compiler)
        {
            _predicate = predicate;
            _compiler = compiler;
        }

        public ConstructedValueExpression Compile(MapContext context)
        {
            if (_predicate(context))
                return _compiler.Compile(context);
            return ConstructedValueExpression.Nothing;
        }
    }
}