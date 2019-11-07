using System;
using System.Linq.Expressions;

namespace CastIron.Sql.Mapping.ScalarCompilers
{
    public class NumericConversionMapCompiler : IScalarMapCompiler
    {
        // They are both numeric but not the same type. Unbox and convert

        public bool CanMap(Type targetType, ColumnInfo column)
            => column.ColumnType.IsNumericType() && targetType.IsNumericType();

        public Expression Map(Type targetType, ColumnInfo column, ParameterExpression rawVar)
            => Expression.Condition(
                Expression.NotEqual(Expressions._dbNullExp, rawVar),
                Expression.Convert(
                    Expression.Unbox(rawVar, column.ColumnType),
                    targetType
                ),
                targetType.GetDefaultValueExpression());
    }
}