namespace CastIron.Sql
{
    /// <summary>
    /// Performance monitor entry. 
    /// </summary>
    public interface IPerformanceEntry
    {
        /// <summary>
        /// The name of the action being measured
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The elapsed time in milliseconds
        /// </summary>
        double TimeMs { get; }
    }
}