using CastIron.Sql.Utility;

namespace CastIron.Sql
{
    public partial class SqlRunnerExtensions
    {
        /// <summary>
        /// Create a batch object which will execute multiple commands and queries in a single connection
        /// for efficiency.
        /// </summary>
        /// <returns></returns>
        public static SqlBatch CreateBatch(this ISqlRunner runner)
        {
            Argument.NotNull(runner, nameof(runner));
            return new SqlBatch(runner.InteractionFactory);
        }

        /// <summary>
        /// Execute all the statements in a batch
        /// </summary>
        /// <param name="runner"></param>
        /// <param name="batch"></param>
        public static void Execute(this ISqlRunner runner, SqlBatch batch)
        {
            Argument.NotNull(runner, nameof(runner));
            Argument.NotNull(batch, nameof(batch));
            runner.Execute(batch.GetExecutors());
        }
    }
}
