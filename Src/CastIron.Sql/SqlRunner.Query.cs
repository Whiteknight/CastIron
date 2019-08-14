using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CastIron.Sql.Execution;
using CastIron.Sql.Statements;
using CastIron.Sql.Utility;

namespace CastIron.Sql
{
    public static partial class SqlRunnerExtensions
    {
        /// <summary>
        /// Execute the SQL code query and return the result. Maps internally to a call to 
        /// IDbCommand.ExecuteReader()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="runner"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static IReadOnlyList<T> Query<T>(this ISqlRunner runner, string sql)
        {
            Argument.NotNull(runner, nameof(runner));
            Argument.NotNullOrEmpty(sql, nameof(sql));
            return runner.Query(SqlQuery.FromString<T>(sql));
        }

        /// <summary>
        /// Execute the SQL code query asynchronously and return the result. Maps internally to a
        /// call to IDbCommand.ExecuteReader()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="runner"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static Task<IReadOnlyList<T>> QueryAsync<T>(this ISqlRunner runner, string sql)
        {
            Argument.NotNull(runner, nameof(runner));
            Argument.NotNullOrEmpty(sql, nameof(sql));
            return runner.QueryAsync(SqlQuery.FromString<T>(sql));
        }

        /// <summary>
        /// Execute the SQL query asynchronously and return the result. Use the provided result
        /// materializer to read the IDataReader and map to result objects. Maps internally to a
        /// call to IDbCommand.ExecuteReader()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="runner"></param>
        /// <param name="query"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static T Query<T>(this ISqlRunner runner, ISqlQuerySimple query, IResultMaterializer<T> reader)
        {
            Argument.NotNull(runner, nameof(runner));
            Argument.NotNull(query, nameof(query));
            Argument.NotNull(reader, nameof(reader));
            return runner.Execute(c => new SqlQuerySimpleStrategy().Execute(query, reader, c, 0));
        }

        /// <summary>
        /// Execute the SQL query asynchronously and return the result. Use the provided result
        /// materializer to read the IDataReader and map to result objects. Maps internally to
        /// a call to IDbCommand.ExecuteReader()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="runner"></param>
        /// <param name="query"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static Task<T> QueryAsync<T>(this ISqlRunner runner, ISqlQuerySimple query, IResultMaterializer<T> reader)
        {
            Argument.NotNull(runner, nameof(runner));
            Argument.NotNull(query, nameof(query));
            Argument.NotNull(reader, nameof(reader));
            return runner.ExecuteAsync(c => new SqlQuerySimpleStrategy().ExecuteAsync(query, reader, c, 0));
        }

        /// <summary>
        /// Execute the query object and return the result. Maps internally to a call to 
        /// IDbCommand.ExecuteReader()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="runner"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public static T Query<T>(this ISqlRunner runner, ISqlQuerySimple<T> query)
        {
            Argument.NotNull(runner, nameof(runner));
            return runner.Query(query, query);
        }

        /// <summary>
        /// Execute the query object asynchronously and return the result. Maps internally to a
        /// call to IDbCommand.ExecuteReader()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="runner"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public static Task<T> QueryAsync<T>(this ISqlRunner runner, ISqlQuerySimple<T> query)
        {
            Argument.NotNull(runner, nameof(runner));
            return runner.QueryAsync(query, query);
        }

        /// <summary>
        /// Execute the SQL query and passes the IDataReader to the provided results materializer
        /// to map to objects. Internally this calls IDbCommand.ExecuteReader()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="runner"></param>
        /// <param name="query"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static T Query<T>(this ISqlRunner runner, ISqlQuery query, IResultMaterializer<T> reader)
        {
            Argument.NotNull(runner, nameof(runner));
            Argument.NotNull(query, nameof(query));
            return runner.Execute(c => new SqlQueryStrategy(runner.InteractionFactory).Execute(query, reader, c, 0));
        }

        /// <summary>
        /// Execute the SQL query and passes the IDataReader to the provided results materializer
        /// callback to map to objects. Internally this calls IDbCommand.ExecuteReader()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="runner"></param>
        /// <param name="query"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static T Query<T>(this ISqlRunner runner, ISqlQuery query, Func<IDataResults, T> reader)
        {
            return Query(runner, query, new DelegateResultMaterializer<T>(reader));
        }

        /// <summary>
        /// Execute the SQL query asynchronously and passes the IDataReader to the provided
        /// results materializer to map to objects. Internally this calls
        /// IDbCommand.ExecuteReader()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="runner"></param>
        /// <param name="query"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static Task<T> QueryAsync<T>(this ISqlRunner runner, ISqlQuery query, IResultMaterializer<T> reader)
        {
            Argument.NotNull(runner, nameof(runner));
            Argument.NotNull(query, nameof(query));
            return runner.ExecuteAsync(c => new SqlQueryStrategy(runner.InteractionFactory).ExecuteAsync(query, reader, c, 0));
        }

        /// <summary>
        /// Execute the SQL query asynchronously and passes the IDataReader to the provided
        /// results materializer callback to map to objects. Internally this calls
        /// IDbCommand.ExecuteReader()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="runner"></param>
        /// <param name="query"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static Task<T> QueryAsync<T>(this ISqlRunner runner, ISqlQuery query, Func<IDataResults, T> reader)
        {
            return QueryAsync(runner, query, new DelegateResultMaterializer<T>(reader));
        }

        /// <summary>
        /// Execute the query object and return the result. Maps internally to a call to 
        /// IDbCommand.ExecuteReader()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="runner"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public static T Query<T>(this ISqlRunner runner, ISqlQuery<T> query)
        {
            Argument.NotNull(runner, nameof(runner));
            return runner.Query(query, query);
        }

        /// <summary>
        /// Execute the query object asynchronously and return the result. Maps internally to a
        /// call to IDbCommand.ExecuteReader()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="runner"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public static Task<T> QueryAsync<T>(this ISqlRunner runner, ISqlQuery<T> query)
        {
            Argument.NotNull(runner, nameof(runner));
            return runner.QueryAsync(query, query);
        }
    }
}
