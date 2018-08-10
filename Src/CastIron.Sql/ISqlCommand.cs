using System.Data;

namespace CastIron.Sql
{
    /// <summary>
    /// Used with .ExecuteNonQuery(), represents a command to update the database without returning any result sets.
    /// </summary>
    public interface ISqlCommand
    {
        bool SetupCommand(IDbCommand command);
    }

    /// <summary>
    /// Used with .ExecuteNonQuery(), represents a command to update the database without returning any
    /// result sets. Is expected to return some kind of result, such as from output parameters
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISqlCommand<out T> : ISqlCommand
    {
        T ReadOutputs(SqlResultSet result);
    }
}