namespace CastIron.Sql
{
    public interface IPerformanceEntry
    {
        string Name { get; }
        double TimeMs { get; }
    }
}