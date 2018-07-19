namespace CastIron.Sql
{
    /// <summary>
    /// A promise-like result from a single statement in a batch. Returns a result after the statement has
    /// been executed
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISqlResult<out T>
    {
        bool IsComplete { get; }
        T GetValue();
        // TODO: Expose a wait handle?
    }

    /// <summary>
    /// A promise-like result from a single statement in a batch. Indicates when the statement has been 
    /// executed
    /// </summary>
    public interface ISqlResult
    {
        bool IsComplete { get; }
        // TODO: Expose a wait handle?
    }
}