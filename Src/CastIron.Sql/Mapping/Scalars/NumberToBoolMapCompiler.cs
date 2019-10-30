using System;
using System.Linq.Expressions;

namespace CastIron.Sql.Mapping.Scalars
{
    public class NumberToBoolMapCompiler : IScalarMapCompiler
    {
        // Target is bool, source type is numeric. Try to coerce by comparing against 0

        public bool CanMap(Type targetType, ColumnInfo column)
            => (targetType == typeof(bool) || targetType == typeof(bool?)) && column.ColumnType.IsNumericType();

        public Expression Map(Type targetType, ColumnInfo column, ParameterExpression rawVar)
            // rawVar != DBNull.Instance ? ((targetType)rawVar != (targetType)0) : false
            => Expression.Condition(
                Expression.NotEqual(Expressions._dbNullExp, rawVar),
                Expression.NotEqual(Expression.Convert(Expression.Constant(0), column.ColumnType), Expression.Unbox(rawVar, column.ColumnType)),
                Expression.Constant(false));
    }
}