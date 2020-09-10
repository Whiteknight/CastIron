using CastIron.Sql.Execution;
using CastIron.Sql.Utility;
using System.Threading;
using System.Threading.Tasks;

namespace CastIron.Sql
{
    public static partial class SqlRunnerExtensions
    {
        /// <summary>
        /// Execute the SQL code query and return a result stream. Maps internally to a call to
        /// IDbCommand.ExecuteReader(). The stream MUST be disposed to avoid a potential memory leak.
        /// </summary>
        /// <param name="runner"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static IDataResultsStream QueryStream(this ISqlRunner runner, string sql)
        {
            Argument.NotNull(runner, nameof(runner));
            Argument.NotNullOrEmpty(sql, nameof(sql));
            return runner.QueryStream(SqlQuery.FromString(sql));
        }

        /// <summary>
        /// Execute the SQL query and return a stream of results. Maps internally to a call to 
        /// IDbCommand.ExecuteReader(). The returned stream object must be disposed when it is finished
        /// being used.
        /// </summary>
        /// <param name="runner"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public static IDataResultsStream QueryStream(this ISqlRunner runner, ISqlQuery query)
        {
            Argument.NotNull(runner, nameof(runner));
            Argument.NotNull(query, nameof(query));
            var context = runner.CreateExecutionContext();
            context.OpenConnection();
            return new SqlQueryStrategy(runner.InteractionFactory).ExecuteStream(query, context);
        }

        /// <summary>
        /// Execute the SQL query and return a stream of results. Maps internally to a call to 
        /// IDbCommand.ExecuteReader(). The returned stream object must be disposed when it is
        /// finished being used.
        /// </summary>
        /// <param name="runner"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public static IDataResultsStream QueryStream(this ISqlRunner runner, ISqlQuerySimple query)
        {
            Argument.NotNull(runner, nameof(runner));
            Argument.NotNull(query, nameof(query));
            var context = runner.CreateExecutionContext();
            context.OpenConnection();
            return new SqlQuerySimpleStrategy().ExecuteStream(query, context);
        }

        /// <summary>
        /// Execute the SQL code query asychronously and return a result stream. Maps internally
        /// to a call to IDbCommand.ExecuteReader(). The stream MUST be disposed to avoid a
        /// potential memory leak.
        /// </summary>
        /// <param name="runner"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static Task<IDataResultsStream> QueryStreamAsync(this ISqlRunner runner, string sql)
        {
            Argument.NotNull(runner, nameof(runner));
            Argument.NotNullOrEmpty(sql, nameof(sql));
            return runner.QueryStreamAsync(SqlQuery.FromString(sql));
        }

        /// <summary>
        /// Execute the SQL query asynchronously and return a stream of results. Maps internally
        /// to a call to IDbCommand.ExecuteReader(). The returned stream object must be disposed
        /// when it is finished being used.
        /// </summary>
        /// <param name="runner"></param>
        /// <param name="query"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<IDataResultsStream> QueryStreamAsync(this ISqlRunner runner, ISqlQuery query, CancellationToken cancellationToken = new CancellationToken())
        {
            Argument.NotNull(runner, nameof(runner));
            Argument.NotNull(query, nameof(query));
            var context = runner.CreateExecutionContext();
            await context.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            return await new SqlQueryStrategy(runner.InteractionFactory).ExecuteStreamAsync(query, context, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Execute the SQL query asynchronously and return a stream of results. Maps internally
        /// to a call to IDbCommand.ExecuteReader(). The returned stream object must be disposed
        /// when it is finished being used.
        /// </summary>
        /// <param name="runner"></param>
        /// <param name="query"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<IDataResultsStream> QueryStreamAsync(this ISqlRunner runner, ISqlQuerySimple query, CancellationToken cancellationToken = new CancellationToken())
        {
            Argument.NotNull(runner, nameof(runner));
            Argument.NotNull(query, nameof(query));
            var context = runner.CreateExecutionContext();
            await context.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            return await new SqlQuerySimpleStrategy().ExecuteStreamAsync(query, context, cancellationToken).ConfigureAwait(false);
        }
    }
}
