namespace CastIron.Sql
{
    public interface ISqlCommand
    {
        string GetSql();
    }

    // TODO: should this implement ISqlParameterized, or is there some other mechanism to get parameters into this?
    public interface ISqlCommand<out T> : ISqlCommand
    {
        T ReadOutputs(SqlQueryRawResultSet result);
    }
}