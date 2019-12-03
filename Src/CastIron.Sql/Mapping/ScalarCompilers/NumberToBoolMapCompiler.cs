using System;
using System.Linq.Expressions;

namespace CastIron.Sql.Mapping.ScalarCompilers
{
    /// <summary>
    /// Compile an expression to convert a numeric value to a boolean result. Zero values are treated
    /// as false, all non-zero values are treated as true.
    /// </summary>
    public class NumberToBoolMapCompiler : IScalarMapCompiler
    {
        // Target is bool, source type is numeric. Try to coerce by comparing against 0

        public bool CanMap(Type targetType, Type columnType, string sqlTypeName)
            => (targetType == typeof(bool) || targetType == typeof(bool?)) && columnType.IsNumericType();

        public Expression Map(Type targetType, Type columnType, string sqlTypeName, ParameterExpression rawVar)
            =>
                // rawVar != DBNull.Instance ? ((targetType)rawVar != (targetType)0) : false
                Expression.Condition(
                    Expression.NotEqual(Expressions.DbNullExp, rawVar),
                    Expression.NotEqual(
                        Expression.Convert(Expression.Constant(0), columnType),
                        Expression.Unbox(rawVar, columnType)
                    ),
                    Expression.Constant(false)
                );
    }
}