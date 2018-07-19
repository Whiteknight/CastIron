using System.Data;

namespace CastIron.Sql
{
    /// <summary>
    /// Used with .ExecuteNonQuery(), represents a command to update the database without returning any result sets.
    /// </summary>
    public interface ISqlCommandRawCommand
    {
        bool SetupCommand(IDbCommand command);
    }

    /// <summary>
    /// Used with .ExecuteNonQuery(), represents a command to update the database without returning any result sets.
    /// Should return an output, such as from Output parameters
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISqlCommandRawCommand<out T> : ISqlCommandRawCommand
    {
        T ReadOutputs(SqlResultSet result);
    }
}