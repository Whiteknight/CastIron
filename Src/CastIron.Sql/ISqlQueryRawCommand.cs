using System.Data;

namespace CastIron.Sql
{
    /// <summary>
    /// Represents a query which returns result sets from the DB. The raw DbCommand is provided
    /// to set query text and parameters manually
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISqlQueryRawCommand<out T>
    {
        bool SetupCommand(IDbCommand command);
        T Read(SqlResultSet result);
    }
}