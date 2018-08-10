namespace CastIron.Sql
{ 
    /// <summary>
    /// Represents a query which returns a result set or multiple result sets
    /// Use ISqlParameterized to add parameters to the query
    /// </summary>
    public interface ISqlQuerySimple<out T>
    {
        string GetSql();
        T Read(SqlResultSet result);
    }
}