namespace CastIron.Sql
{
    /// <summary>
    /// Used with .ExecuteNonQuery(), represents a command to update the database without returning any result sets.
    /// </summary>
    public interface ISqlCommand
    {
        /// <summary>
        /// Setup the IDataInteraction command with SQL and parameters
        /// </summary>
        /// <param name="interaction"></param>
        /// <returns></returns>
        bool SetupCommand(IDataInteraction interaction);
    }

    /// <summary>
    /// Used with .ExecuteNonQuery(), represents a command to update the database without returning any
    /// result sets. Is expected to return some kind of result, such as from output parameters
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISqlCommand<out T> : ISqlCommand
    {
        /// <summary>
        /// Read the command outputs, such as output parameters or command metadata
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        T ReadOutputs(IDataResults result);
    }
}