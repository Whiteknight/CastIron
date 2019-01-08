namespace CastIron.Sql
{
    /// <summary>
    /// Represents an SQL query which will be executed with command.ExecuteQuery()
    /// </summary>
    public interface ISqlQuery 
    {
        /// <summary>
        /// Setup the data interaction including SQL code and parameters
        /// </summary>
        /// <param name="interaction"></param>
        /// <returns></returns>
        bool SetupCommand(IDataInteraction interaction);
    }

    /// <summary>
    /// Represents a query which returns result sets from the DB. The raw DbCommand is provided
    /// to set query text and parameters manually
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISqlQuery<out T> : ISqlQuery
    {
        /// <summary>
        /// Get results from the query
        /// </summary>
        /// <param name="result">The result object which contains data reader, mappers, and output parameters</param>
        /// <returns></returns>
        T Read(IDataResults result);
    }
}