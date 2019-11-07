namespace CastIron.Sql.Mapping
{
    public interface ICompiler
    {
        ConstructedValueExpression Compile(MapState state);
    }
}