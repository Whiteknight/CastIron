namespace CastIron.Sql
{
    public interface ISqlQuery 
    {
        bool SetupCommand(IDataInteraction interaction);
    }

    /// <summary>
    /// Represents a query which returns result sets from the DB. The raw DbCommand is provided
    /// to set query text and parameters manually
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISqlQuery<out T> : ISqlQuery
    {
        T GetResults(IDataResults result);
    }
}