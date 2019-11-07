namespace CastIron.Sql.Mapping.Compilers
{
    public class FailCompiler : ICompiler
    {
        public ConstructedValueExpression Compile(MapState state)
        {
            return ConstructedValueExpression.Nothing;
        }
    }
}