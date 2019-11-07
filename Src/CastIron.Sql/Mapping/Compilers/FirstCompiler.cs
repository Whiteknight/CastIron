using System.Collections.Generic;

namespace CastIron.Sql.Mapping.Compilers
{
    public class FirstCompiler : ICompiler
    {
        private readonly IReadOnlyList<ICompiler> _compilers;

        public FirstCompiler(params ICompiler[] compilers)
        {
            _compilers = compilers;
        }

        public ConstructedValueExpression Compile(MapState state)
        {
            for (int i = 0; i < _compilers.Count; i++)
            {
                var result = _compilers[i].Compile(state);
                if (!result.IsNothing)
                    return result;
            }
            return ConstructedValueExpression.Nothing;
        }
    }
}