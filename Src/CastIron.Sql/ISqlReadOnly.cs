namespace CastIron.Sql
{
    /// <summary>
    /// Tag to indicate that a Query or Command does not make modifications and would prefer to be executed
    /// on a read-only node if possible
    /// </summary>
    public interface ISqlReadOnly
    {
    }
}