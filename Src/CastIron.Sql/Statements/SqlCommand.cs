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

        public override int GetHashCode() => _sql.GetHashCode();

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (!(obj is SqlCommand typed))
                return false;
            return _sql == typed._sql;
        }
    }
}