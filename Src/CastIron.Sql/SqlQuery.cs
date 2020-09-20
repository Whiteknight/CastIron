using System;
using System.Collections.Generic;
using CastIron.Sql.Mapping;
using CastIron.Sql.Statements;

namespace CastIron.Sql
{
    public static class SqlQuery
    {
        public static ISqlQuerySimple FromString(string sql)
            => new SqlQuerySimpleFromString(sql);

        public static ISqlQuerySimple<IReadOnlyList<T>> FromString<T>(string sql, Action<IMapCompilerSettings<T>> setupCompiler = null)
            => new SqlQuerySimpleFromString<T>(sql, setupCompiler);

        public static ISqlQuery FromSimple(ISqlQuerySimple simple)
            => new SqlQueryFromSimple(simple);

        public static ISqlQuery<T> FromSimple<T>(ISqlQuerySimple<T> simple)
            => new SqlQueryFromSimple<T>(simple);

        public static ISqlQuerySimple<T> Combine<T>(string sql, IResultMaterializer<T> reader)
            => Combine(new SqlQuerySimpleFromString(sql), reader);

        public static ISqlQuerySimple<T> Combine<T>(string sql, Func<IDataResults, T> materialize)
            => Combine(new SqlQuerySimpleFromString(sql), new DelegateResultMaterializer<T>(materialize));

        public static ISqlQuerySimple<T> Combine<T>(ISqlQuerySimple query, IResultMaterializer<T> reader)
            => new SqlQuerySimpleCombined<T>(query, reader);

        public static ISqlQuerySimple<T> Combine<T>(ISqlQuerySimple query, Func<IDataResults, T> materialize)
            => new SqlQuerySimpleCombined<T>(query, new DelegateResultMaterializer<T>(materialize));

        public static ISqlQuery<T> Combine<T>(ISqlQuery query, IResultMaterializer<T> reader)
            => new SqlQueryCombined<T>(query, reader);

        public static ISqlQuery<T> Combine<T>(ISqlQuery query, Func<IDataResults, T> materialize)
            => new SqlQueryCombined<T>(query, new DelegateResultMaterializer<T>(materialize));

        public static IResultMaterializer<T> FromDelegate<T>(Func<IDataResults, T> materialize)
            => new DelegateResultMaterializer<T>(materialize);
    }
}