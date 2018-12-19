using CastIron.Sql.Utility;

namespace CastIron.Sql.Statements
{
    public class SqlCommand : ISqlCommandSimple
    {
        private readonly string _sql;

        public SqlCommand(string sql)
        {
            Assert.ArgumentNotNullOrEmpty(sql, nameof(sql));
            _sql = sql;
        }

        public string GetSql()
        {
            return _sql;
        }
    }
}