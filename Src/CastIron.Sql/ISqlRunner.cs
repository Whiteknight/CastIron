using System;
using System.Collections.Generic;
using System.Data;
using CastIron.Sql.Mapping;
using CastIron.Sql.Statements;
using CastIron.Sql.Utility;

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

        /// <summary>
        /// Execute the SQL query and return a stream of results. Maps internally to a call to 
        /// IDbCommand.ExecuteReader(). The returned stream object must be disposed when it is finished
        /// being used.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="build"></param>
        /// <returns></returns>
        IDataResultsStream QueryStream(ISqlQuery query, Action<IContextBuilder> build = null);

        /// <summary>
        /// Execute the SQL query and return a stream of results. Maps internally to a call to 
        /// IDbCommand.ExecuteReader(). The returned stream object must be disposed when it is finished
        /// being used.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="build"></param>
        /// <returns></returns>
        IDataResultsStream QueryStream(ISqlQuerySimple query, Action<IContextBuilder> build = null);
    }

    public static class SqlRunnerExtensions
    {
        /// <summary>
        /// Execute a raw string of SQL and return no result. Maps internally to a call to 
        /// IDbCommand.ExecuteNonQuery()
        /// </summary>
        /// <param name="runner"></param>
        /// <param name="sql"></param>
        /// <param name="build"></param>
        public static void Execute(this ISqlRunner runner, string sql, Action<IContextBuilder> build = null)
        {
            Assert.ArgumentNotNull(runner, nameof(runner));
            Assert.ArgumentNotNullOrEmpty(sql, nameof(sql));

            runner.Execute(new SqlCommand(sql), build);
        }

        /// <summary>
        /// Execute the SQL code query and return the result. Maps internally to a call to 
        /// IDbCommand.ExecuteReader()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="runner"></param>
        /// <param name="sql"></param>
        /// <param name="build"></param>
        /// <returns></returns>
        public static IReadOnlyList<T> Query<T>(this ISqlRunner runner, string sql, Action<IContextBuilder> build = null)
        {
            Assert.ArgumentNotNull(runner, nameof(runner));
            Assert.ArgumentNotNullOrEmpty(sql, nameof(sql));

            return runner.Query(new SqlQuery<T>(sql), build);
        }

        /// <summary>
        /// Execute the SQL code query and return a result stream. Maps internally to a call to
        /// IDbCommand.ExecuteReader(). The stream MUST be disposed to avoid a potential memory leak.
        /// </summary>
        /// <param name="runner"></param>
        /// <param name="sql"></param>
        /// <param name="build"></param>
        /// <returns></returns>
        public static IDataResultsStream QueryStream(this ISqlRunner runner, string sql, Action<IContextBuilder> build = null)
        {
            Assert.ArgumentNotNull(runner, nameof(runner));
            Assert.ArgumentNotNullOrEmpty(sql, nameof(sql));

            return runner.QueryStream(new SqlQuery(sql), build);
        }

        /// <summary>
        /// Wraps an existing IDataReader in an IDataResultsStream for object mapping and other capabilities.
        /// The IDataReader (and IDbCommand and IDbConnection, if any) will need to be managed and disposed
        /// manually.
        /// </summary>
        /// <param name="runner"></param>
        /// <param name="reader"></param>
        /// <param name="command">If provided, gives access to mapped output parameters</param>
        /// <returns></returns>
        public static IDataResultsStream WrapAsResultStream(this ISqlRunner runner, IDataReader reader, IDbCommand command = null)
        {
            return new SqlDataReaderResultStream(command, null, reader);
        }

        /// <summary>
        /// Wraps an existing DataTable in an IDataResultsStream for object mapping and other capabilities.
        /// Any objects used to produce the data table, such as IDbCommand and IDbConnection must be 
        /// managed and disposed manually.
        /// </summary>
        /// <param name="runner"></param>
        /// <param name="table"></param>
        /// <returns></returns>
        public static IDataResultsStream WrapAsResultStream(this ISqlRunner runner, DataTable table)
        {
            Assert.ArgumentNotNull(table, nameof(table));
            return new SqlDataReaderResultStream(null, null, table.CreateDataReader());

        }
    }
}