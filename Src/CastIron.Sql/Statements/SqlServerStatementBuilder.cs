namespace CastIron.Sql.Statements
{
    public class SqlServerStatementBuilder : ISqlStatementBuilder
    {
        public ISqlSelectQuery<T> GetSelectStatement<T>()
        {
            return new SqlSelectQuery<T>();
        }
    }
}