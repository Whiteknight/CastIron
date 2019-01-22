using System;
using System.Collections.Generic;
using System.Linq;
using CastIron.Sql.Mapping;
using CastIron.Sql.Utility;

namespace CastIron.Sql.Statements
{
    public class SqlQuery : ISqlQuerySimple
    {
        private readonly string _sql;

        public SqlQuery(string sql)
        {
            Assert.ArgumentNotNullOrEmpty(sql, nameof(sql));
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
        private readonly Action<IMapCompilerBuilder<TRow>> _setupCompiler;

        public SqlQuery(string sql, Action<IMapCompilerBuilder<TRow>> setupCompiler = null)
        {
            Assert.ArgumentNotNullOrEmpty(sql, nameof(sql));
            _sql = sql;
            _setupCompiler = setupCompiler;
        }

        public string GetSql()
        {
            return _sql;
        }

        public IReadOnlyList<TRow> Read(IDataResults result)
        {
            return result.AsEnumerable(_setupCompiler).ToList();
        }
    }
}