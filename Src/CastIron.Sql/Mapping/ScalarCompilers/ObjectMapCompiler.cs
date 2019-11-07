using System;
using System.Linq.Expressions;

namespace CastIron.Sql.Mapping.ScalarCompilers
{
    public class ObjectMapCompiler : IScalarMapCompiler
    {
        public bool CanMap(Type targetType, ColumnInfo column) => targetType == typeof(object);

        public Expression Map(Type targetType, ColumnInfo column, ParameterExpression rawVar)
            => Expression.Condition(
                Expression.NotEqual(Expressions._dbNullExp, rawVar),
                rawVar,
                typeof(object).GetDefaultValueExpression());
    }
}