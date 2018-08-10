using System;
using System.Collections.Generic;

namespace CastIron.Sql
{
    public interface ISqlRunner
    {
        QueryObjectStringifier Stringifier { get; }
        void Execute(SqlBatch batch, Action<IContextBuilder> build = null);
        void Execute(string sql, Action<IContextBuilder> build = null);
        T Query<T>(ISqlQuerySimple<T> query, Action<IContextBuilder> build = null);
        T Query<T>(ISqlQuery<T> query, Action<IContextBuilder> build = null);
        T Query<T>(ISqlConnectionAccessor<T> query, Action<IContextBuilder> build = null);
        IReadOnlyList<T> Query<T>(string sql, Action<IContextBuilder> build = null);
        void Execute(ISqlCommandSimple command, Action<IContextBuilder> build = null);
        T Execute<T>(ISqlCommandSimple<T> command, Action<IContextBuilder> build = null);
        void Execute(ISqlCommand command, Action<IContextBuilder> build = null);
        T Execute<T>(ISqlCommand<T> command, Action<IContextBuilder> build = null);
    }
}