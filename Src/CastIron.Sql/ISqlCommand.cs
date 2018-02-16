namespace CastIron.Sql
{
    public interface ISqlCommand
    {
        string GetSql();
    }

    public interface ISqlCommand<out T> : ISqlCommand
    {
        T ReadOutputs(SqlQueryResult result);
    }
}