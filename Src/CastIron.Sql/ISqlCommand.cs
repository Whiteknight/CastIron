namespace CastIron.Sql
{
    /// <summary>
    /// Used with .ExecuteNonQuery(), represents a command to modify the database without returning a result set
    /// </summary>
    public interface ISqlCommand
    {
        string GetSql();
    }

    /// <summary>
    /// Used with .ExecuteNonQuery(), represents a command to modify the database without returning a result set.
    /// This variant is expected to return some kind of result value, such as output parameters through the use
    /// of ISqlParameterized
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISqlCommand<out T> : ISqlCommand
    {
        T ReadOutputs(SqlResultSet result);
    }
}