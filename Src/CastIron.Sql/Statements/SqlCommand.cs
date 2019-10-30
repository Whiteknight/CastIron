using CastIron.Sql.Utility;

namespace CastIron.Sql.Statements
{
    public class SqlCommand : ISqlCommandSimple
    {
        private readonly string _sql;

        public SqlCommand(string sql)
        {
            Argument.NotNullOrEmpty(sql, nameof(sql));
            _sql = sql;
        }

        public string GetSql() => _sql;
    }
}