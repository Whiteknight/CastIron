namespace CastIron.Sql
{
    /// <summary>
    /// Tag to indicate that a query or command type represents a stored procedure. The SQL
    /// command text is treated as the name of the stored procedure, and parameters to the query/command
    /// will be passed as parameters to the procedure.
    /// </summary>
    public interface ISqlStoredProc
    {

    }
}