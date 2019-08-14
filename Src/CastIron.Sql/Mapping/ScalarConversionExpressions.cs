using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping
{
    public static class ScalarConversionExpressions
    {
        private static readonly MethodInfo _getValueMethod = typeof(IDataRecord).GetMethod(nameof(IDataRecord.GetValue));
        private static readonly Expression _dbNullExp = Expression.Field(null, typeof(DBNull), nameof(DBNull.Value));
        private static readonly MethodInfo _convertMethod = typeof(Convert).GetMethod(nameof(Convert.ChangeType), new[] { typeof(object), typeof(Type) });
        private static readonly MethodInfo _guidParseMethod = typeof(Guid).GetMethod(nameof(Guid.Parse));

        public static ConstructedValueExpression Get(int columnIdx, MapCompileContext context, ColumnInfo column, Type targetType)
        { 
            // TODO: If the column is a serialized type like XML or JSON, we should be able to deserialize that into an object.
            var columnType = column.ColumnType;
            var expressions = new List<Expression>();

            // Pull the value out of the reader into the rawVar
            var rawVar = context.AddVariable<object>("raw");
            var getRawStmt = Expression.Assign(rawVar, Expression.Call(context.RecordParam, _getValueMethod, Expression.Constant(columnIdx)));
            expressions.Add(getRawStmt);

            var rawConvertExpr = GetRawValueConversionExpression(targetType, rawVar, columnType);
            return new ConstructedValueExpression(expressions, rawConvertExpr);
        }

        private static Expression GetRawValueConversionExpression(Type targetType, ParameterExpression rawVar, Type columnType)
        {
            if (targetType == typeof(object))
            {
                // raw != DBNull.Instance ? raw : (object)null;
                return Expression.Condition(
                    Expression.NotEqual(_dbNullExp, rawVar),
                    rawVar,
                    typeof(object).GetDefaultValueExpression());
            }

            // They are the same type, so we can directly assign them
            if (columnType == targetType || (!columnType.IsClass && typeof(Nullable<>).MakeGenericType(columnType) == targetType))
            {
                // raw != DBNull.Instance ? (targetType)raw : default(targetType);
                return Expression.Condition(
                    Expression.NotEqual(_dbNullExp, rawVar),
                    Expression.Convert(
                        rawVar,
                        targetType
                    ),
                    targetType.GetDefaultValueExpression());
            }

            // The target is string, regardless of the data type we can .ToString() it
            if (targetType == typeof(string))
            {
                // raw != DBNull.Instance ? raw.ToString() : (string)null;
                return Expression.Condition(
                    Expression.NotEqual(_dbNullExp, rawVar),
                    Expression.Call(rawVar, nameof(ToString), Type.EmptyTypes),
                    typeof(string).GetDefaultValueExpression());
            }

            // They are both numeric but not the same type. Unbox and convert
            if (columnType.IsNumericType() && targetType.IsNumericType())
            {
                return Expression.Condition(
                    Expression.NotEqual(_dbNullExp, rawVar),
                    Expression.Convert(
                        Expression.Unbox(rawVar, columnType),
                        targetType
                    ),
                    targetType.GetDefaultValueExpression());
            }

            // Target is bool, source type is numeric. Try to coerce by comparing against 0
            if ((targetType == typeof(bool) || targetType == typeof(bool?)) && columnType.IsNumericType())
            {
                // rawVar != DBNull.Instance ? ((targetType)rawVar != (targetType)0) : false
                return Expression.Condition(
                    Expression.NotEqual(_dbNullExp, rawVar),
                    Expression.NotEqual(Expression.Convert(Expression.Constant(0), columnType), Expression.Unbox(rawVar, columnType)),
                    Expression.Constant(false));
            }

            if (columnType == typeof(string) && (targetType == typeof(Guid) || targetType == typeof(Guid?)))
            {
                return Expression.Condition(
                    Expression.NotEqual(_dbNullExp, rawVar),
                    Expression.Convert(
                        Expression.Call(null, _guidParseMethod, Expression.Convert(rawVar, typeof(string))), targetType),
                    targetType.GetDefaultValueExpression());
            }

            // Convert.ChangeType where the column is IConvertable. Convert to the non-Nullable<> version of TargetType
            if (columnType.IsConvertible())
            {
                // raw != DBNull.Instance ? (targetType)Convert.ChangeType(raw, targetType) : default(targetType)
                var baseTargetType = targetType.GetTypeWithoutNullability();
                if (targetType.IsConvertible())
                {
                    return Expression.Condition(
                        Expression.NotEqual(_dbNullExp, rawVar),
                        Expression.Convert(
                            Expression.Call(null, _convertMethod, new Expression[] { rawVar, Expression.Constant(baseTargetType, typeof(Type)) }), targetType),
                        targetType.GetDefaultValueExpression());
                }
            }

            // There is no conversion rule, so just return a default value.
            return targetType.GetDefaultValueExpression();
        }
    }
}