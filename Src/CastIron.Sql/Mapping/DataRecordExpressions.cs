using System;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping
{
    public static class DataRecordExpressions
    {
        private static readonly MethodInfo GetValueMethod = typeof(IDataRecord).GetMethod(nameof(IDataRecord.GetValue));
        private static readonly Expression _dbNullExp = Expression.Field(null, typeof(DBNull), nameof(DBNull.Value));

        public static Expression GetConversionExpression(int columnIdx, DataRecordMapperCompileContext context, Type columnType, Type targetType)
        {
            // Pull the value out of the reader into the rawVar
            var rawVar = Expression.Variable(typeof(object), "raw_" + columnIdx);
            context.Variables.Add(rawVar);
            var getRawStmt = Expression.Assign(rawVar, Expression.Call(context.RecordParam, GetValueMethod, Expression.Constant(columnIdx)));
            context.Statements.Add(getRawStmt);

            // TODO: Should we store the converted values into variables instead of inlining them?
            // TODO: If the target property is a concrete collection type like List<T> we can do the item conversion and then wrap it in the collection

            // The target is string, regardless of the data type we can .ToString() it
            if (targetType == typeof(string))
            {
                return Expression.Condition(
                    Expression.NotEqual(_dbNullExp, rawVar),
                    Expression.Call(rawVar, nameof(ToString), Type.EmptyTypes),
                    Expression.Convert(Expression.Constant(null), typeof(string)));
            }

            // They are the same type, so we can directly assign them
            if (columnType == targetType || (!columnType.IsClass && typeof(Nullable<>).MakeGenericType(columnType) == targetType))
            {
                return Expression.Condition(
                    Expression.NotEqual(_dbNullExp, rawVar),
                    Expression.Convert(
                        rawVar,
                        targetType
                    ),
                    Expression.Convert(Expression.Constant(GetDefaultValue(targetType)), targetType));
            }

            // They are both numeric but not the same type. Unbox and convert
            if (CompilerTypes.Numeric.Contains(columnType) && CompilerTypes.Numeric.Contains(targetType))
            {
                return Expression.Condition(
                    Expression.NotEqual(_dbNullExp, rawVar),
                    Expression.Convert(
                        Expression.Unbox(rawVar, columnType),
                        targetType
                    ),
                    Expression.Convert(Expression.Constant(GetDefaultValue(targetType)), targetType));
            }

            // Target is bool, source type is numeric. Try to coerce by comparing against 0
            if ((targetType == typeof(bool) || targetType == typeof(bool?)) && CompilerTypes.Numeric.Contains(columnType))
            {
                // var result = rawVar is DBNull ? (rawVar != 0) : false
                return Expression.Condition(
                    Expression.NotEqual(_dbNullExp, rawVar),
                    Expression.NotEqual(Expression.Convert(Expression.Constant(0), columnType), Expression.Unbox(rawVar, columnType)),
                    Expression.Constant(false));
            }

            // We fall back to a basic conversion and hope all goes well
            return Expression.Condition(
                Expression.NotEqual(_dbNullExp, rawVar),
                Expression.Convert(
                    rawVar,
                    targetType
                ),
                Expression.Convert(Expression.Constant(GetDefaultValue(targetType)), targetType));
        }

        public static object GetDefaultValue(Type t)
        {
            return t.IsValueType ? Activator.CreateInstance(t) : null;
        }
    }
}