using System;
using System.Linq.Expressions;

namespace CastIron.Sql.Mapping.ScalarCompilers
{
    public class ToStringMapCompiler : IScalarMapCompiler
    {
        // The target is string, regardless of the data type we can .ToString() it

        public bool CanMap(Type targetType, ColumnInfo column) => targetType == typeof(string);

        public Expression Map(Type targetType, ColumnInfo column, ParameterExpression rawVar)
            => Expression.Condition(
                Expression.NotEqual(Expressions._dbNullExp, rawVar),
                Expression.Call(rawVar, nameof(ToString), Type.EmptyTypes),
                typeof(string).GetDefaultValueExpression());
    }
}