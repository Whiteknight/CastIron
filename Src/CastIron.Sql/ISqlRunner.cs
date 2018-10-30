using System;
using System.Collections.Generic;

namespace CastIron.Sql
{
    /// <summary>
    /// Runner object to execute ISqlCommand and ISqlQuery variants. Creates a connection to the data 
    /// provider, executes commands and queries, and manages the return of results.
    /// </summary>
    public interface ISqlRunner
    {
        /// <summary>
        /// A stringifier object which can be used to turn an ISqlCommand or ISqlQuery into human-readable
        /// SQL code for debugging and auditing purposes.
        /// </summary>
        QueryObjectStringifier Stringifier { get; }

        /// <summary>
        /// A builder object to help with building simple, standard queries
        /// </summary>
        ISqlStatementBuilder Statements { get; }

        /// <summary>
        /// Create a batch object which will execute multiple commands and queries in a single connection
        /// for efficiency.
        /// </summary>
        /// <returns></returns>
        SqlBatch CreateBatch();

        /// <summary>
        /// Execute all the statements in a batch
        /// </summary>
        /// <param name="batch"></param>
        /// <param name="build"></param>
        void Execute(SqlBatch batch, Action<IContextBuilder> build = null);

        /// <summary>
        /// Execute a raw string of SQL and return no result. Maps internally to a call to 
        /// IDbCommand.ExecuteNonQuery()
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="build"></param>
        void Execute(string sql, Action<IContextBuilder> build = null);

        /// <summary>
        /// Execute the query object and return the result. Maps internally to a call to 
        /// IDbCommand.ExecuteReader()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="build"></param>
        /// <returns></returns>
        T Query<T>(ISqlQuerySimple<T> query, Action<IContextBuilder> build = null);

        /// <summary>
        /// Execute the query object and return the result. Maps internally to a call to 
        /// IDbCommand.ExecuteReader()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="build"></param>
        /// <returns></returns>
        T Query<T>(ISqlQuery<T> query, Action<IContextBuilder> build = null);

        /// <summary>
        /// Execute the query object and return the result. Maps internally to a call to 
        /// IDbCommand.ExecuteReader()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="accessor"></param>
        /// <param name="build"></param>
        /// <returns></returns>
        T Query<T>(ISqlConnectionAccessor<T> accessor, Action<IContextBuilder> build = null);

        /// <summary>
        /// Establish a connection to the provider and pass control of the connection to an accessor object 
        /// for low-level manipulation
        /// </summary>
        /// <param name="accessor"></param>
        /// <param name="build"></param>
        void Execute(ISqlConnectionAccessor accessor, Action<IContextBuilder> build = null);

        /// <summary>
        /// Execute the SQL code query and return the result. Maps internally to a call to 
        /// IDbCommand.ExecuteReader()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="build"></param>
        /// <returns></returns>
        IReadOnlyList<T> Query<T>(string sql, Action<IContextBuilder> build = null);

        /// <summary>
        /// Execute the command and return no result. Maps internally to a call to 
        /// IDbCommand.ExecuteNonQuery()
        /// </summary>
        /// <param name="command"></param>
        /// <param name="build"></param>
        void Execute(ISqlCommandSimple command, Action<IContextBuilder> build = null);

        /// <summary>
        /// Execute the command and return a result. Commands will not
        /// generate an IDataReader or IDataResults, so results will either need to be calculated or
        /// derived from output parameters. Maps internally to a call to IDbCommand.ExecuteNonQuery()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command"></param>
        /// <param name="build"></param>
        /// <returns></returns>
        T Execute<T>(ISqlCommandSimple<T> command, Action<IContextBuilder> build = null);

        /// <summary>
        /// Execute the command and return no result. Maps internally to a call to 
        /// IDbCommand.ExecuteNonQuery()
        /// </summary>
        /// <param name="command"></param>
        /// <param name="build"></param>
        void Execute(ISqlCommand command, Action<IContextBuilder> build = null);

        /// <summary>
        /// Execute the command and return a result. Commands will not
        /// generate an IDataReader or IDataResults, so results will either need to be calculated or
        /// derived from output parameters. Maps internally to a call to IDbCommand.ExecuteNonQuery()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command"></param>
        /// <param name="build"></param>
        /// <returns></returns>
        T Execute<T>(ISqlCommand<T> command, Action<IContextBuilder> build = null);

        IDataResultsStream QueryStream(ISqlQuery query, Action<IContextBuilder> build = null);
        IDataResultsStream QueryStream(ISqlQuerySimple query, Action<IContextBuilder> build = null);
        IDataResultsStream QueryStream(string sql, Action<IContextBuilder> build = null);
    }
}