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

        public static ISqlQuerySimple<IReadOnlyList<T>> FromString<T>(string sql, Action<IMapCompilerSettings> setup = null, bool cacheMappings = false)
            => new SqlQuerySimpleFromString<T>(sql, setup, cacheMappings);

        public static ISqlQuery FromSimple(ISqlQuerySimple simple)
            => new SqlQueryFromSimple(simple);

        public static ISqlQuery<T> FromSimple<T>(ISqlQuerySimple<T> simple)
            => new SqlQueryFromSimple<T>(simple);

        public static ISqlQuerySimple<T> Combine<T>(string sql, IResultMaterializer<T> reader, bool cacheMappings = false)
            => Combine(new SqlQuerySimpleFromString(sql), reader, cacheMappings);

        public static ISqlQuerySimple<T> Combine<T>(string sql, Func<IDataResults, T> materialize, bool cacheMappings = false)
            => Combine(new SqlQuerySimpleFromString(sql), new DelegateResultMaterializer<T>(materialize), cacheMappings);

        public static ISqlQuerySimple<T> Combine<T>(ISqlQuerySimple query, IResultMaterializer<T> materializer, bool cacheMappings = false)
            => new SqlQuerySimpleCombined<T>(query, materializer, cacheMappings);

        public static ISqlQuerySimple<T> Combine<T>(ISqlQuerySimple query, Func<IDataResults, T> materialize, bool cacheMappings = false)
            => new SqlQuerySimpleCombined<T>(query, new DelegateResultMaterializer<T>(materialize), cacheMappings);

        public static ISqlQuery<T> Combine<T>(ISqlQuery query, IResultMaterializer<T> materializer, bool cacheMappings = false)
            => new SqlQueryCombined<T>(query, materializer, cacheMappings);

        public static ISqlQuery<T> Combine<T>(ISqlQuery query, Func<IDataResults, T> materialize, bool cacheMappings = false)
            => new SqlQueryCombined<T>(query, new DelegateResultMaterializer<T>(materialize), cacheMappings);
    }

    public static class Materializer
    {
        public static IResultMaterializer<T> FromDelegate<T>(Func<IDataResults, T> materialize)
            => new DelegateResultMaterializer<T>(materialize);
    }
}