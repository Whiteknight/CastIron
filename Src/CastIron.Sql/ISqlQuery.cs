namespace CastIron.Sql
{
    public interface ISqlQueryBase
    {

    }

    public interface ISqlQuery<out T> : ISqlQueryBase
    {
        string GetSql();
        T Read(SqlQueryRawResultSet result);
    }
}