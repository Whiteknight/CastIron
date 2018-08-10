using System.Data;

namespace CastIron.Sql
{
    /// <summary>
    /// A very low-level query object to return result sets from the database. The raw IDbConnection is 
    /// provided  on which any number of commands or queries can be executed to return a value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISqlConnectionAccessor<out T> 
    {
        T Query(IDbConnection connection, IDbTransaction transaction);
    }

    /// <summary>
    /// A very low-level command object to execute changes in the database. The raw IDbConnection is provided
    /// on which any number of commands or queries can be executed (though no results are expected)
    /// </summary>
    public interface ISqlConnectionAccessor
    {
        void Execute(IDbConnection connection, IDbTransaction transaction);
    }
}