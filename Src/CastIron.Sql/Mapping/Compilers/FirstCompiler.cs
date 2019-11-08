using System.Collections.Generic;

namespace CastIron.Sql.Mapping.Compilers
{
    /// <summary>
    /// Executes compilers in order and returns as soon as any compiler succeeds. If no compilers succeed,
    /// a failure result is returned
    /// </summary>
    public class FirstCompiler : ICompiler
    {
        private readonly IReadOnlyList<ICompiler> _compilers;

        public FirstCompiler(params ICompiler[] compilers)
        {
            _compilers = compilers;
        }

        public ConstructedValueExpression Compile(MapContext context)
        {
            for (int i = 0; i < _compilers.Count; i++)
            {
                var result = _compilers[i].Compile(context);
                if (!result.IsNothing)
                    return result;
            }
            return ConstructedValueExpression.Nothing;
        }
    }
}