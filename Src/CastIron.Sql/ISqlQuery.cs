namespace CastIron.Sql
{
    public interface ISqlQuery<out T>
    {
        string GetSql();
        T Read(SqlQueryResult result);
    }
}