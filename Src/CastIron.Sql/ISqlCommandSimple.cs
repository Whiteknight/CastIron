namespace CastIron.Sql
{
    /// <summary>
    /// Used with .ExecuteNonQuery(), represents a simple command to modify the database without returning a 
    /// result set
    /// </summary>
    public interface ISqlCommandSimple
    {
        /// <summary>
        /// Get a string of SQL to execute
        /// </summary>
        /// <returns></returns>
        string GetSql();
    }

    /// <summary>
    /// Used with .ExecuteNonQuery(), represents a command to modify the database without returning a result 
    /// set.  This variant is expected to return some kind of result value, such as output parameters.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISqlCommandSimple<out T> : ISqlCommandSimple
    {
        /// <summary>
        /// Return outputs from the command including output parameter values and command metadata
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        T ReadOutputs(IDataResults result);
    }
}