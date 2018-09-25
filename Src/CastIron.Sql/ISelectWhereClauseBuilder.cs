using System;
using System.Linq.Expressions;

namespace CastIron.Sql
{
    public interface ISelectWhereClauseBuilder<T>
    {
        void And(params Action<ISelectWhereClauseBuilder<T>>[] conditions);
        void Or(params Action<ISelectWhereClauseBuilder<T>>[] conditions);
        void Equal<TProperty>(Expression<Func<T, TProperty>> property, object value);
        void Equal(string property, object value);
        void NotEqual<TProperty>(Expression<Func<T, TProperty>> property, object value);
        void NotEqual(string property, object value);
        void GreaterThan<TProperty>(Expression<Func<T, TProperty>> property, object value);
        void GreaterThan(string property, object value);
        void GreaterThanOrEqual<TProperty>(Expression<Func<T, TProperty>> property, object value);
        void GreaterThanOrEqual(string property, object value);
        void LessThan<TProperty>(Expression<Func<T, TProperty>> property, object value);
        void LessThan(string property, object value);
        void LessThanOrEqual<TProperty>(Expression<Func<T, TProperty>> property, object value);
        void LessThanOrEqual(string property, object value);
        void Like<TProperty>(Expression<Func<T, TProperty>> property, string value);
        void Like(string property, string value);
        void Between<TProperty>(Expression<Func<T, TProperty>> property, object value1, object value2);
        void Between(string property, object value1, object value2);
    }
}