using System.Collections.Generic;
using System.Linq;

namespace CastIron.Sql.Statements
{
    public class SqlQuery<TRow> : ISqlQuery<IReadOnlyList<TRow>>
    {
        private readonly string _sql;

        public SqlQuery(string sql)
        {
            _sql = sql;
        }

        public string GetSql()
        {
            return _sql;
        }

        public IReadOnlyList<TRow> Read(SqlResultSet result)
        {
            return result.AsEnumerable<TRow>().ToList();
        }
    }
}