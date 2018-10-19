namespace CastIron.Sql
{
    /// <summary>
    /// Used with .ExecuteNonQuery(), represents a simple command to modify the database without returning a 
    /// result set
    /// </summary>
    public interface ISqlCommandSimple
    {
        string GetSql();
    }

    /// <summary>
    /// Used with .ExecuteNonQuery(), represents a command to modify the database without returning a result 
    /// set.  This variant is expected to return some kind of result value, such as output parameters.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISqlCommandSimple<out T> : ISqlCommandSimple
    {
        T ReadOutputs(IDataResults result);
    }
}