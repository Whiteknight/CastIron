using System.Collections.Generic;
using System.Linq;

namespace CastIron.Sql.Statements
{
    public class SqlQuery : ISqlQuerySimple
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
    }

    public class SqlQuery<TRow> : ISqlQuerySimple<IReadOnlyList<TRow>>
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

        public IReadOnlyList<TRow> Read(IDataResults result)
        {
            return result.AsEnumerable<TRow>().ToList();
        }
    }
}