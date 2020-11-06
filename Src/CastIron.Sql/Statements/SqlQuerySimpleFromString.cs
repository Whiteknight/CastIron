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

        public override int GetHashCode() => _sql.GetHashCode();

        public override bool Equals(object obj) => obj != null && obj is SqlQuerySimpleFromString typed && typed._sql == _sql;
    }

    public class SqlQuerySimpleFromString<TRow> : ISqlQuerySimple<IReadOnlyList<TRow>>
    {
        private readonly string _sql;
        private readonly bool _cacheMappings;
        private readonly Action<IMapCompilerSettings> _setupCompiler;

        public SqlQuerySimpleFromString(string sql, Action<IMapCompilerSettings> setupCompiler = null, bool cacheMappings = false)
        {
            Argument.NotNullOrEmpty(sql, nameof(sql));
            _sql = sql;
            _cacheMappings = cacheMappings;
            _setupCompiler = setupCompiler;
        }

        public string GetSql() => _sql;

        public IReadOnlyList<TRow> Read(IDataResults result)
        {
            result.CacheMappings(_cacheMappings);
            return result.AsEnumerable<TRow>(_setupCompiler).ToList();
        }

        public override int GetHashCode()
        {
            if (_setupCompiler != null)
                return _sql.GetHashCode() ^ _setupCompiler.GetHashCode() ^ _cacheMappings.GetHashCode();
            return _sql.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is SqlQuerySimpleFromString<TRow> typed))
                return false;
            if (_setupCompiler != null)
                return _sql == typed._sql && _setupCompiler == typed._setupCompiler;
            return _sql == typed._sql;
        }
    }
}