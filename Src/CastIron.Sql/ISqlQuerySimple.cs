namespace CastIron.Sql
{ 
    /// <summary>
    /// A simple SQL query which represents a raw string of SQL to execute
    /// </summary>
    public interface ISqlQuerySimple
    {
        /// <summary>
        /// Get the SQL for the query
        /// </summary>
        /// <returns></returns>
        string GetSql();
    }

    /// <summary>
    /// Represents a query which returns a result set or multiple result sets
    /// Use ISqlParameterized to add parameters to the query
    /// </summary>
    public interface ISqlQuerySimple<out T> : ISqlQuerySimple, ISqlQueryReader<T>
    {
    }
}