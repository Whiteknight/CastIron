using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using CastIron.Sql.Execution;
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
        /// Factory to create IDataInteraction objects to abstract details of the underlying connection
        /// and command objects
        /// </summary>
        IDataInteractionFactory InteractionFactory { get; }

        /// <summary>
        /// Create a batch object which will execute multiple commands and queries in a single connection
        /// for efficiency.
        /// </summary>
        /// <returns></returns>
        SqlBatch CreateBatch();

        /// <summary>
        /// Create an execution context for a database connection. The context provides parameters
        /// and utilities to help manage the connection
        /// </summary>
        /// <param name="build"></param>
        /// <returns></returns>
        ExecutionContext CreateExecutionContext(Action<IContextBuilder> build);

        /// <summary>
        /// Open a connection to the database and execute a list of executors on the open connection
        /// </summary>
        /// <param name="executors"></param>
        /// <param name="build"></param>
        void Execute(IReadOnlyList<Action<IExecutionContext, int>> executors, Action<IContextBuilder> build);

        /// <summary>
        /// Open a connection to the database and execute an executor on the open connection. The
        /// executor is expected to return a result
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="executor"></param>
        /// <param name="build"></param>
        /// <returns></returns>
        T Execute<T>(Func<IExecutionContext, T> executor, Action<IContextBuilder> build);

        /// <summary>
        /// Open a connection to the database and execute an executor on the open connection. The
        /// executor is not expected to return a result
        /// </summary>
        /// <param name="executor"></param>
        /// <param name="build"></param>
        void Execute(Action<IExecutionContext> executor, Action<IContextBuilder> build);

        Task<T> ExecuteAsync<T>(Func<IExecutionContext, Task<T>> executor, Action<IContextBuilder> build);

        Task ExecuteAsync(Func<IExecutionContext, Task> executor, Action<IContextBuilder> build);
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

        public static Task<IReadOnlyList<T>> QueryAsync<T>(this ISqlRunner runner, string sql, Action<IContextBuilder> build = null)
        {
            Assert.ArgumentNotNull(runner, nameof(runner));
            Assert.ArgumentNotNullOrEmpty(sql, nameof(sql));

            return runner.QueryAsync(new SqlQuery<T>(sql), build);
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

        public static Task<IDataResultsStream> QueryStreamAsync(this ISqlRunner runner, string sql, Action<IContextBuilder> build = null)
        {
            Assert.ArgumentNotNull(runner, nameof(runner));
            Assert.ArgumentNotNullOrEmpty(sql, nameof(sql));

            return runner.QueryStreamAsync(new SqlQuery(sql), build);
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
            return new SqlDataReaderResultsStream(command, null, reader);
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
            return new SqlDataReaderResultsStream(null, null, table.CreateDataReader());

        }

        /// <summary>
        /// Execute all the statements in a batch
        /// </summary>
        /// <param name="runner"></param>
        /// <param name="batch"></param>
        /// <param name="build"></param>
        public static void Execute(this ISqlRunner runner, SqlBatch batch, Action<IContextBuilder> build = null)
        {
            Assert.ArgumentNotNull(runner, nameof(runner));
            Assert.ArgumentNotNull(batch, nameof(batch));
            runner.Execute(batch.GetExecutors(), build);
        }

        public static T Query<T>(this ISqlRunner runner, ISqlQuerySimple query, ISqlQueryReader<T> reader, Action<IContextBuilder> build = null)
        {
            Assert.ArgumentNotNull(runner, nameof(runner));
            Assert.ArgumentNotNull(query, nameof(query));
            Assert.ArgumentNotNull(reader, nameof(reader));
            return runner.Execute(c => new SqlQuerySimpleStrategy().Execute(query, reader, c, 0), build);
        }

        public static Task<T> QueryAsync<T>(this ISqlRunner runner, ISqlQuerySimple query, ISqlQueryReader<T> reader, Action<IContextBuilder> build = null)
        {
            Assert.ArgumentNotNull(runner, nameof(runner));
            Assert.ArgumentNotNull(query, nameof(query));
            Assert.ArgumentNotNull(reader, nameof(reader));
            return runner.ExecuteAsync(c => new SqlQuerySimpleStrategy().ExecuteAsync(query, reader, c, 0), build);
        }

        /// <summary>
        /// Execute the query object and return the result. Maps internally to a call to 
        /// IDbCommand.ExecuteReader()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="runner"></param>
        /// <param name="query"></param>
        /// <param name="build"></param>
        /// <returns></returns>
        public static T Query<T>(this ISqlRunner runner, ISqlQuerySimple<T> query, Action<IContextBuilder> build = null)
        {
            Assert.ArgumentNotNull(runner, nameof(runner));
            return runner.Query<T>(query, query, build);
        }

        public static Task<T> QueryAsync<T>(this ISqlRunner runner, ISqlQuerySimple<T> query, Action<IContextBuilder> build = null)
        {
            Assert.ArgumentNotNull(runner, nameof(runner));
            return runner.QueryAsync<T>(query, query, build);
        }

        public static T Query<T>(this ISqlRunner runner, ISqlQuery query, ISqlQueryReader<T> reader, Action<IContextBuilder> build = null)
        {
            Assert.ArgumentNotNull(runner, nameof(runner));
            Assert.ArgumentNotNull(query, nameof(query));
            return runner.Execute(c => new SqlQueryStrategy(runner.InteractionFactory).Execute(query, reader, c, 0), build);
        }

        public static Task<T> QueryAsync<T>(this ISqlRunner runner, ISqlQuery query, ISqlQueryReader<T> reader, Action<IContextBuilder> build = null)
        {
            Assert.ArgumentNotNull(runner, nameof(runner));
            Assert.ArgumentNotNull(query, nameof(query));
            return runner.ExecuteAsync(c => new SqlQueryStrategy(runner.InteractionFactory).ExecuteAsync(query, reader, c, 0), build);
        }

        /// <summary>
        /// Execute the query object and return the result. Maps internally to a call to 
        /// IDbCommand.ExecuteReader()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="runner"></param>
        /// <param name="query"></param>
        /// <param name="build"></param>
        /// <returns></returns>
        public static T Query<T>(this ISqlRunner runner, ISqlQuery<T> query, Action<IContextBuilder> build = null)
        {
            Assert.ArgumentNotNull(runner, nameof(runner));
            return runner.Query<T>(query, query, build);
        }

        public static Task<T> QueryAsync<T>(this ISqlRunner runner, ISqlQuery<T> query, Action<IContextBuilder> build = null)
        {
            Assert.ArgumentNotNull(runner, nameof(runner));
            return runner.QueryAsync<T>(query, query, build);
        }

        /// <summary>
        /// Execute the query object and return the result. Maps internally to a call to 
        /// IDbCommand.ExecuteReader()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="runner"></param>
        /// <param name="accessor"></param>
        /// <param name="build"></param>
        /// <returns></returns>
        public static T Access<T>(this ISqlRunner runner, ISqlConnectionAccessor<T> accessor, Action<IContextBuilder> build = null)
        {
            Assert.ArgumentNotNull(runner, nameof(runner));
            Assert.ArgumentNotNull(accessor, nameof(accessor));
            return runner.Execute(c => new SqlConnectionAccessorStrategy().Execute(accessor, c, 0), build);
        }

        /// <summary>
        /// Establish a connection to the provider and pass control of the connection to an accessor object 
        /// for low-level manipulation
        /// </summary>
        /// <param name="runner"></param>
        /// <param name="accessor"></param>
        /// <param name="build"></param>
        public static void Access(this ISqlRunner runner, ISqlConnectionAccessor accessor, Action<IContextBuilder> build = null)
        {
            Assert.ArgumentNotNull(runner, nameof(runner));
            Assert.ArgumentNotNull(accessor, nameof(accessor));
            runner.Execute(c => new SqlConnectionAccessorStrategy().Execute(accessor, c, 0), build);
        }

        /// <summary>
        /// Execute the command and return no result. Maps internally to a call to 
        /// IDbCommand.ExecuteNonQuery()
        /// </summary>
        /// <param name="runner"></param>
        /// <param name="command"></param>
        /// <param name="build"></param>
        public static void Execute(this ISqlRunner runner, ISqlCommandSimple command, Action<IContextBuilder> build = null)
        {
            Assert.ArgumentNotNull(runner, nameof(runner));
            Assert.ArgumentNotNull(command, nameof(command));
            runner.Execute(c => new SqlCommandSimpleStrategy().Execute(command, c, 0), build);
        }

        public static Task ExecuteAsync(this ISqlRunner runner, ISqlCommandSimple command, Action<IContextBuilder> build = null)
        {
            Assert.ArgumentNotNull(runner, nameof(runner));
            Assert.ArgumentNotNull(command, nameof(command));
            return runner.ExecuteAsync(c => new SqlCommandSimpleStrategy().ExecuteAsync(command, c, 0), build);
        }

        /// <summary>
        /// Execute the command and return a result. Commands will not
        /// generate an IDataReader or IDataResults, so results will either need to be calculated or
        /// derived from output parameters. Maps internally to a call to IDbCommand.ExecuteNonQuery()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="runner"></param>
        /// <param name="command"></param>
        /// <param name="build"></param>
        /// <returns></returns>
        public static T Execute<T>(this ISqlRunner runner, ISqlCommandSimple<T> command, Action<IContextBuilder> build = null)
        {
            Assert.ArgumentNotNull(runner, nameof(runner));
            Assert.ArgumentNotNull(command, nameof(command));
            return runner.Execute(c => new SqlCommandSimpleStrategy().Execute(command, c, 0), build);
        }

        public static Task<T> ExecuteAsync<T>(this ISqlRunner runner, ISqlCommandSimple<T> command, Action<IContextBuilder> build = null)
        {
            Assert.ArgumentNotNull(runner, nameof(runner));
            Assert.ArgumentNotNull(command, nameof(command));
            return runner.ExecuteAsync(c => new SqlCommandSimpleStrategy().ExecuteAsync(command, c, 0), build);
        }

        /// <summary>
        /// Execute the command and return no result. Maps internally to a call to 
        /// IDbCommand.ExecuteNonQuery()
        /// </summary>
        /// <param name="runner"></param>
        /// <param name="command"></param>
        /// <param name="build"></param>
        public static void Execute(this ISqlRunner runner, ISqlCommand command, Action<IContextBuilder> build = null)
        {
            Assert.ArgumentNotNull(runner, nameof(runner));
            Assert.ArgumentNotNull(command, nameof(command));
            runner.Execute(c => new SqlCommandStrategy(runner.InteractionFactory).Execute(command, c, 0), build);
        }

        public static Task ExecuteAsync(this ISqlRunner runner, ISqlCommand command, Action<IContextBuilder> build = null)
        {
            Assert.ArgumentNotNull(runner, nameof(runner));
            Assert.ArgumentNotNull(command, nameof(command));
            return runner.ExecuteAsync(c => new SqlCommandStrategy(runner.InteractionFactory).ExecuteAsync(command, c, 0), build);
        }

        /// <summary>
        /// Execute the command and return a result. Commands will not
        /// generate an IDataReader or IDataResults, so results will either need to be calculated or
        /// derived from output parameters. Maps internally to a call to IDbCommand.ExecuteNonQuery()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="runner"></param>
        /// <param name="command"></param>
        /// <param name="build"></param>
        /// <returns></returns>
        public static T Execute<T>(this ISqlRunner runner, ISqlCommand<T> command, Action<IContextBuilder> build = null)
        {
            Assert.ArgumentNotNull(runner, nameof(runner));
            Assert.ArgumentNotNull(command, nameof(command));
            return runner.Execute(c => new SqlCommandStrategy(runner.InteractionFactory).Execute(command, c, 0), build);
        }

        public static Task<T> ExecuteAsync<T>(this ISqlRunner runner, ISqlCommand<T> command, Action<IContextBuilder> build = null)
        {
            Assert.ArgumentNotNull(runner, nameof(runner));
            Assert.ArgumentNotNull(command, nameof(command));
            return runner.ExecuteAsync(c => new SqlCommandStrategy(runner.InteractionFactory).ExecuteAsync(command, c, 0), build);
        }

        /// <summary>
        /// Execute the SQL query and return a stream of results. Maps internally to a call to 
        /// IDbCommand.ExecuteReader(). The returned stream object must be disposed when it is finished
        /// being used.
        /// </summary>
        /// <param name="runner"></param>
        /// <param name="query"></param>
        /// <param name="build"></param>
        /// <returns></returns>
        public static IDataResultsStream QueryStream(this ISqlRunner runner, ISqlQuery query, Action<IContextBuilder> build = null)
        {
            Assert.ArgumentNotNull(runner, nameof(runner));
            Assert.ArgumentNotNull(query, nameof(query));
            var context = runner.CreateExecutionContext(build);
            context.StartAction("Open connection");
            context.OpenConnection();
            return new SqlQueryStrategy(runner.InteractionFactory).ExecuteStream(query, context);
        }

        public static async Task<IDataResultsStream> QueryStreamAsync(this ISqlRunner runner, ISqlQuery query, Action<IContextBuilder> build = null)
        {
            Assert.ArgumentNotNull(runner, nameof(runner));
            Assert.ArgumentNotNull(query, nameof(query));
            var context = runner.CreateExecutionContext(build);
            context.StartAction("Open connection");
            await context.OpenConnectionAsync();
            return await new SqlQueryStrategy(runner.InteractionFactory).ExecuteStreamAsync(query, context);
        }

        /// <summary>
        /// Execute the SQL query and return a stream of results. Maps internally to a call to 
        /// IDbCommand.ExecuteReader(). The returned stream object must be disposed when it is finished
        /// being used.
        /// </summary>
        /// <param name="runner"></param>
        /// <param name="query"></param>
        /// <param name="build"></param>
        /// <returns></returns>
        public static IDataResultsStream QueryStream(this ISqlRunner runner, ISqlQuerySimple query, Action<IContextBuilder> build = null)
        {
            Assert.ArgumentNotNull(runner, nameof(runner));
            Assert.ArgumentNotNull(query, nameof(query));
            var context = runner.CreateExecutionContext(build);
            context.StartAction("Open connection");
            context.OpenConnection();
            return new SqlQuerySimpleStrategy().ExecuteStream(query, context);
        }

        public static async Task<IDataResultsStream> QueryStreamAsync(this ISqlRunner runner, ISqlQuerySimple query, Action<IContextBuilder> build = null)
        {
            Assert.ArgumentNotNull(runner, nameof(runner));
            Assert.ArgumentNotNull(query, nameof(query));
            var context = runner.CreateExecutionContext(build);
            context.StartAction("Open connection");
            await context.OpenConnectionAsync();
            return await new SqlQuerySimpleStrategy().ExecuteStreamAsync(query, context);
        }
    }
}