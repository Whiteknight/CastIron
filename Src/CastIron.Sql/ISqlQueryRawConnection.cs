using System.Data;

namespace CastIron.Sql
{
    /// <summary>
    /// A very low-level query to return result sets from the database. The raw IDbConnection is provided
    /// on which any number of commands or queries can be executed to return a value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISqlQueryRawConnection<out T> 
    {
        T Query(IDbConnection connection, IDbTransaction transaction);
    }
}