namespace CastIron.Sql
{
    public interface ISqlResult<out T>
    {
        bool IsComplete { get; }
        T GetValue();
    }

    public interface ISqlResult
    {
        bool IsComplete { get; }
    }
}