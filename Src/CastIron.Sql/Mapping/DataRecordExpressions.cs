using System;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping
{
    public static class DataRecordExpressions
    {
        private static readonly MethodInfo _getValueMethod = typeof(IDataRecord).GetMethod(nameof(IDataRecord.GetValue));
        private static readonly Expression _dbNullExp = Expression.Field(null, typeof(DBNull), nameof(DBNull.Value));
        private static readonly MethodInfo _convertMethod = typeof(Convert).GetMethod(nameof(Convert.ChangeType), new[] { typeof(object), typeof(Type) });
        private static readonly MethodInfo _guidParseMethod = typeof(Guid).GetMethod(nameof(Guid.Parse));

        public static Expression GetScalarConversionExpression(int columnIdx, MapCompileContext context, Type columnType, Type targetType)
        { 
            // Pull the value out of the reader into the rawVar
            var rawVar = context.AddVariable<object>("raw");
            var getRawStmt = Expression.Assign(rawVar, Expression.Call(context.RecordParam, _getValueMethod, Expression.Constant(columnIdx)));
            context.AddStatement(getRawStmt);

            if (targetType == typeof(object))
            {
                // raw != DBNull.Instance ? raw : (object)null;
                return Expression.Condition(
                    Expression.NotEqual(_dbNullExp, rawVar),
                    rawVar,
                    GetDefaultValueExpression(typeof(object))); 
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
                    GetDefaultValueExpression(targetType));
            }

            // The target is string, regardless of the data type we can .ToString() it
            if (targetType == typeof(string))
            {
                // raw != DBNull.Instance ? raw.ToString() : (string)null;
                return Expression.Condition(
                    Expression.NotEqual(_dbNullExp, rawVar),
                    Expression.Call(rawVar, nameof(ToString), Type.EmptyTypes),
                    GetDefaultValueExpression(typeof(string)));
            }

            // They are both numeric but not the same type. Unbox and convert
            if (DataTypes.IsNumericType(columnType) && DataTypes.IsNumericType(targetType))
            {
                return Expression.Condition(
                    Expression.NotEqual(_dbNullExp, rawVar),
                    Expression.Convert(
                        Expression.Unbox(rawVar, columnType),
                        targetType
                    ),
                    GetDefaultValueExpression(targetType));
            }

            // Target is bool, source type is numeric. Try to coerce by comparing against 0
            if ((targetType == typeof(bool) || targetType == typeof(bool?)) && DataTypes.IsNumericType(columnType))
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
                        Expression.Call(null, _guidParseMethod, new Expression[] {
                            Expression.Convert(rawVar, typeof(string)) }), targetType),
                    GetDefaultValueExpression(targetType));
            }

            // Convert.ChangeType where the column is IConvertable. Convert to the non-Nullable<> version of TargetType
            if (IsConvertible(columnType))
            {
                // raw != DBNull.Instance ? (targetType)Convert.ChangeType(raw, targetType) : default(targetType)
                var baseTargetType = GetTypeWithoutNullability(targetType);
                if (IsConvertible(targetType))
                {
                    return Expression.Condition(
                        Expression.NotEqual(_dbNullExp, rawVar),
                        Expression.Convert(
                            Expression.Call(null, _convertMethod, new Expression[] { rawVar, Expression.Constant(baseTargetType, typeof(Type)) }), targetType),
                        GetDefaultValueExpression(targetType));
                }
            }
            
            // There is no conversion rule, so just return a default value.
            return GetDefaultValueExpression(targetType);
        }

        

        public static Expression GetDefaultValueExpression(Type t)
        {
            // (t)default(t)
            return Expression.Convert(Expression.Constant(DataTypes.GetDefaultValue(t)), t);
        }

        private static bool IsConvertible(Type t)
        {
            return typeof(IConvertible).IsAssignableFrom(t);
        }

        private static Type GetTypeWithoutNullability(Type t)
        {
            return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>) ? t.GenericTypeArguments[0] : t;
        }
    }
}