namespace CastIron.Sql.Statements
{
    public class SqlStatementBuilder : ISqlStatementBuilder
    {
        public ISqlSelectQuery<T> GetSelectStatement<T>()
        {
            return new SqlSelectQuery<T>();
        }
    }
}