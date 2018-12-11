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
        /// <summary>
        /// Perform some operation of the initialized connection and transaction objects. Some result is expected.
        /// </summary>
        /// <param name="connection">The open connection object</param>
        /// <param name="transaction">The current transaction object or null</param>
        /// <returns></returns>
        T Query(IDbConnection connection, IDbTransaction transaction);
    }

    /// <summary>
    /// A very low-level command object to execute changes in the database. The raw IDbConnection is provided
    /// on which any number of commands or queries can be executed (though no results are expected)
    /// </summary>
    public interface ISqlConnectionAccessor
    {
        /// <summary>
        /// Perform some operation on the initialized connection and transaction objects. No result is expected
        /// </summary>
        /// <param name="connection">The open connection object</param>
        /// <param name="transaction">The current transaction object or null</param>
        void Execute(IDbConnection connection, IDbTransaction transaction);
    }
}