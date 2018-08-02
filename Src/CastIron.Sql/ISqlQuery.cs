using System.Data;

namespace CastIron.Sql
{ 
    /// <summary>
    /// Represents a query which returns a result set or multiple result sets
    /// Use ISqlParameterized to add parameters to the query
    /// </summary>
    public interface ISqlQuery<out T>
    {
        string GetSql();
        T Read(SqlResultSet result);
    }

    public interface ISqlQueryParameterized<out T, in TParameters>
    {
        void SetupCommand(IDbCommand command);
        void SetValues(IDbCommand command, TParameters parameters);
        T Read(SqlResultSet result);
    }
}