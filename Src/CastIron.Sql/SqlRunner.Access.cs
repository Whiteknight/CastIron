using CastIron.Sql.Execution;
using CastIron.Sql.Utility;

namespace CastIron.Sql
{
    public static partial class SqlRunnerExtensions
    {
        /// <summary>
        /// Execute the query object and return the result. Maps internally to a call to 
        /// IDbCommand.ExecuteReader()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="runner"></param>
        /// <param name="accessor"></param>

        /// <returns></returns>
        public static T Access<T>(this ISqlRunner runner, ISqlConnectionAccessor<T> accessor)
        {
            Argument.NotNull(runner, nameof(runner));
            Argument.NotNull(accessor, nameof(accessor));
            return runner.Execute(c => new SqlConnectionAccessorStrategy().Execute(accessor, c, 0));
        }

        /// <summary>
        /// Establish a connection to the provider and pass control of the connection to an accessor object 
        /// for low-level manipulation
        /// </summary>
        /// <param name="runner"></param>
        /// <param name="accessor"></param>
        public static void Access(this ISqlRunner runner, ISqlConnectionAccessor accessor)
        {
            Argument.NotNull(runner, nameof(runner));
            Argument.NotNull(accessor, nameof(accessor));
            runner.Execute(c => new SqlConnectionAccessorStrategy().Execute(accessor, c, 0));
        }
    }
}
