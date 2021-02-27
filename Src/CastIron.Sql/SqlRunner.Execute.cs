using System;
using System.Threading;
using System.Threading.Tasks;
using CastIron.Sql.Execution;
using CastIron.Sql.Statements;
using CastIron.Sql.Utility;

namespace CastIron.Sql
{
    public static partial class SqlRunnerExtensions
    {
        /// <summary>
        /// Execute a raw string of SQL and return no result. Maps internally to a call to
        /// IDbCommand.ExecuteNonQuery()
        /// </summary>
        /// <param name="runner"></param>
        /// <param name="sql"></param>
        public static void Execute(this ISqlRunner runner, string sql)
        {
            Argument.NotNull(runner, nameof(runner));
            Argument.NotNullOrEmpty(sql, nameof(sql));
            runner.Execute(new SqlCommand(sql));
        }

        /// <summary>
        /// Execute the command and return no result. Maps internally to a call to
        /// IDbCommand.ExecuteNonQuery()
        /// </summary>
        /// <param name="runner"></param>
        /// <param name="command"></param>
        public static void Execute(this ISqlRunner runner, ISqlCommandSimple command)
        {
            Argument.NotNull(runner, nameof(runner));
            Argument.NotNull(command, nameof(command));
            runner.Execute(c => new SqlCommandSimpleStrategy().Execute(command, c, 0));
        }

        /// <summary>
        /// Execute the command and return a result. Commands will not
        /// generate an IDataReader or IDataResults, so results will either need to be calculated or
        /// derived from output parameters. Maps internally to a call to IDbCommand.ExecuteNonQuery()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="runner"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        public static T Execute<T>(this ISqlRunner runner, ISqlCommandSimple<T> command)
        {
            Argument.NotNull(runner, nameof(runner));
            Argument.NotNull(command, nameof(command));
            return runner.Execute(c => new SqlCommandSimpleStrategy().Execute(command, c, 0));
        }

        /// <summary>
        /// Execute the command and return no result. Maps internally to a call to
        /// IDbCommand.ExecuteNonQuery()
        /// </summary>
        /// <param name="runner"></param>
        /// <param name="command"></param>
        public static void Execute(this ISqlRunner runner, ISqlCommand command)
        {
            Argument.NotNull(runner, nameof(runner));
            Argument.NotNull(command, nameof(command));
            runner.Execute(c => new SqlCommandStrategy(runner.InteractionFactory).Execute(command, c, 0));
        }

        /// <summary>
        /// Execute the command and return a result. Commands will not
        /// generate an IDataReader or IDataResults, so results will either need to be calculated or
        /// derived from output parameters. Maps internally to a call to IDbCommand.ExecuteNonQuery()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="runner"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        public static T Execute<T>(this ISqlRunner runner, ISqlCommand<T> command)
        {
            Argument.NotNull(runner, nameof(runner));
            Argument.NotNull(command, nameof(command));
            return runner.Execute(c => new SqlCommandStrategy(runner.InteractionFactory).Execute(command, c, 0));
        }

        public static void Execute(this ISqlRunner runner, Func<IDataInteraction, bool> setup)
        {
            Argument.NotNull(runner, nameof(runner));
            Argument.NotNull(setup, nameof(setup));
            runner.Execute(new SqlCommandFromDelegate(setup));
        }

        /// <summary>
        /// Execute the command asynchronously and return no result. Maps internally to a call to
        /// IDbCommand.ExecuteNonQuery()
        /// </summary>
        /// <param name="runner"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        public static Task ExecuteAsync(this ISqlRunner runner, ISqlCommandSimple command, CancellationToken cancellationToken = new CancellationToken())
        {
            Argument.NotNull(runner, nameof(runner));
            Argument.NotNull(command, nameof(command));
            return runner.ExecuteAsync(c => new SqlCommandSimpleStrategy().ExecuteAsync(command, c, 0, cancellationToken));
        }

        /// <summary>
        /// Execute the command asynchronously and return a result. Commands will not
        /// generate an IDataReader or IDataResults, so results will either need to be calculated or
        /// derived from output parameters. Maps internally to a call to IDbCommand.ExecuteNonQuery()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="runner"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        public static Task<T> ExecuteAsync<T>(this ISqlRunner runner, ISqlCommandSimple<T> command, CancellationToken cancellationToken = new CancellationToken())
        {
            Argument.NotNull(runner, nameof(runner));
            Argument.NotNull(command, nameof(command));
            return runner.ExecuteAsync(c => new SqlCommandSimpleStrategy().ExecuteAsync(command, c, 0, cancellationToken));
        }

        /// <summary>
        /// Execute the command asynchronously and return no result. Maps internally to a call to
        /// IDbCommand.ExecuteNonQuery()
        /// </summary>
        /// <param name="runner"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        public static Task ExecuteAsync(this ISqlRunner runner, ISqlCommand command, CancellationToken cancellationToken = new CancellationToken())
        {
            Argument.NotNull(runner, nameof(runner));
            Argument.NotNull(command, nameof(command));
            return runner.ExecuteAsync(c => new SqlCommandStrategy(runner.InteractionFactory).ExecuteAsync(command, c, 0, cancellationToken));
        }

        /// <summary>
        /// Execute the command and return a result. Commands will not
        /// generate an IDataReader or IDataResults, so results will either need to be calculated or
        /// derived from output parameters. Maps internally to a call to IDbCommand.ExecuteNonQuery()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="runner"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        public static Task<T> ExecuteAsync<T>(this ISqlRunner runner, ISqlCommand<T> command, CancellationToken cancellationToken = new CancellationToken())
        {
            Argument.NotNull(runner, nameof(runner));
            Argument.NotNull(command, nameof(command));
            return runner.ExecuteAsync(c => new SqlCommandStrategy(runner.InteractionFactory).ExecuteAsync(command, c, 0, cancellationToken));
        }
    }
}
