using System;
using System.Collections.Generic;
using System.Linq;
using CastIron.Sql.Mapping;
using CastIron.Sql.Utility;

namespace CastIron.Sql.Statements
{
    public class SqlQuerySimpleFromString : ISqlQuerySimple
    {
        private readonly string _sql;

        public SqlQuerySimpleFromString(string sql)
        {
            Argument.NotNullOrEmpty(sql, nameof(sql));
            _sql = sql;
        }

        public string GetSql() => _sql;
    }

    public class SqlQuerySimpleFromString<TRow> : ISqlQuerySimple<IReadOnlyList<TRow>>
    {
        private readonly string _sql;
        private readonly Action<IMapCompilerSettings> _setupCompiler;

        public SqlQuerySimpleFromString(string sql, Action<IMapCompilerSettings> setupCompiler = null)
        {
            Argument.NotNullOrEmpty(sql, nameof(sql));
            _sql = sql;
            _setupCompiler = setupCompiler;
        }

        public string GetSql() => _sql;

        public IReadOnlyList<TRow> Read(IDataResults result)
            => result.AsEnumerable<TRow>(_setupCompiler).ToList();
    }
}