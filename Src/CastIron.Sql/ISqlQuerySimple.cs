namespace CastIron.Sql
{ 
    public interface ISqlQuerySimple
    {
        string GetSql();
    }
    /// <summary>
    /// Represents a query which returns a result set or multiple result sets
    /// Use ISqlParameterized to add parameters to the query
    /// </summary>
    public interface ISqlQuerySimple<out T> : ISqlQuerySimple
    {
        T Read(IDataResults result);
    }
}