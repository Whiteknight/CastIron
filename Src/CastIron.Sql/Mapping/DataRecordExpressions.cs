using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping
{
    public static class DataRecordExpressions
    {
        private static readonly HashSet<Type> _numericTypes = new HashSet<Type>
        {
            typeof(byte),
            typeof(byte?),
            typeof(decimal),
            typeof(decimal?),
            typeof(double),
            typeof(double?),
            typeof(float),
            typeof(float?),
            typeof(int),
            typeof(int?),
            typeof(long),
            typeof(long?),
            typeof(short),
            typeof(short?),
            typeof(uint),
            typeof(uint?),
            typeof(ulong),
            typeof(ulong?),
            typeof(ushort),
            typeof(ushort?),
        };

        private static readonly HashSet<Type> _primitiveTypes = new HashSet<Type>
        {
            // TODO: Are there any other types we can map from the DB?
            typeof(bool),
            typeof(bool?),
            typeof(byte),
            typeof(byte?),
            typeof(byte[]),
            typeof(DateTime),
            typeof(DateTime?),
            typeof(decimal),
            typeof(decimal?),
            typeof(double),
            typeof(double?),
            typeof(float),
            typeof(float?),
            typeof(Guid),
            typeof(Guid?),
            typeof(int),
            typeof(int?),
            typeof(long),
            typeof(long?),
            typeof(short),
            typeof(short?),
            typeof(string),
            typeof(uint),
            typeof(uint?),
            typeof(ulong),
            typeof(ulong?),
            typeof(ushort),
            typeof(ushort?),
        };

        private static readonly MethodInfo _getValueMethod = typeof(IDataRecord).GetMethod(nameof(IDataRecord.GetValue));
        private static readonly Expression _dbNullExp = Expression.Field(null, typeof(DBNull), nameof(DBNull.Value));
        private static readonly MethodInfo _convertMethod = typeof(Convert).GetMethod(nameof(Convert.ChangeType), new[] { typeof(object), typeof(Type) });
        private static readonly MethodInfo _guidParseMethod = typeof(Guid).GetMethod(nameof(Guid.Parse));

        public static bool IsSupportedPrimitiveType(Type t)
        {
            return _primitiveTypes.Contains(t);
        }

        public static bool IsNumericType(Type t)
        {
            return _numericTypes.Contains(t);
        }

        public static bool IsMappableScalarType(Type t)
        {
            return _primitiveTypes.Contains(t) || t == typeof(object);
        }

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
            if (_numericTypes.Contains(columnType) && _numericTypes.Contains(targetType))
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
            if ((targetType == typeof(bool) || targetType == typeof(bool?)) && _numericTypes.Contains(columnType))
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

        public static object GetDefaultValue(Type t)
        {
            return t.IsValueType ? Activator.CreateInstance(t) : null;
        }

        public static Expression GetDefaultValueExpression(Type t)
        {
            // (t)default(t)
            return Expression.Convert(Expression.Constant(GetDefaultValue(t)), t);
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