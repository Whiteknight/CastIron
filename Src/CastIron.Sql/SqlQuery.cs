﻿using System;
using System.Collections.Generic;
using CastIron.Sql.Mapping;
using CastIron.Sql.Statements;

namespace CastIron.Sql
{
    public static class SqlQuery
    {
        public static ISqlQuerySimple Simple(string sql)
        {
            return new SqlQuerySimpleFromString(sql);
        }

        public static ISqlQuerySimple<IReadOnlyList<T>> Simple<T>(string sql, Action<IMapCompilerBuilder<T>> setupCompiler = null)
        {
            return new SqlQuerySimpleFromString<T>(sql, setupCompiler);
        }

        public static ISqlQuerySimple<T> Combine<T>(string sql, IResultMaterializer<T> reader)
        {
            return Combine(new SqlQuerySimpleFromString(sql), reader);
        }

        public static ISqlQuerySimple<T> Combine<T>(string sql, Func<IDataResults, T> materialize)
        {
            return Combine(new SqlQuerySimpleFromString(sql), new DelegateResultMaterializer<T>(materialize));
        }

        public static ISqlQuerySimple<T> Combine<T>(ISqlQuerySimple query, IResultMaterializer<T> reader)
        {
            return new SqlQuerySimpleCombined<T>(query, reader);
        }

        public static ISqlQuerySimple<T> Combine<T>(ISqlQuerySimple query, Func<IDataResults, T> materialize)
        {
            return new SqlQuerySimpleCombined<T>(query, new DelegateResultMaterializer<T>(materialize));
        }

        public static ISqlQuery<T> Combine<T>(ISqlQuery query, IResultMaterializer<T> reader)
        {
            return new SqlQueryCombined<T>(query, reader);
        }

        public static ISqlQuery<T> Combine<T>(ISqlQuery query, Func<IDataResults, T> materialize)
        {
            return new SqlQueryCombined<T>(query, new DelegateResultMaterializer<T>(materialize));
        }

        public static IResultMaterializer<T> Wrap<T>(Func<IDataResults, T> materialize)
        {
            return new DelegateResultMaterializer<T>(materialize);
        }
    }
}