namespace CastIron.Sql
{
    public interface ISqlStatementBuilder
    {
        ISqlSelectQuery<T> GetSelectStatement<T>();
    }
}