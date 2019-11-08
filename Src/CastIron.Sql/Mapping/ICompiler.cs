namespace CastIron.Sql.Mapping
{
    /// <summary>
    /// Compiler type which, given the current map state, constructs a list of expressions for mapping
    /// columns to objects
    /// </summary>
    public interface ICompiler
    {
        ConstructedValueExpression Compile(MapContext context);
    }
}