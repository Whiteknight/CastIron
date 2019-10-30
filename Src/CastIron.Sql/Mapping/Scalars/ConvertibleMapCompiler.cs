using System;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping.Scalars
{
    public class ConvertibleMapCompiler : IScalarMapCompiler
    {
        private static readonly MethodInfo _convertMethod = typeof(Convert).GetMethod(nameof(Convert.ChangeType), new[] { typeof(object), typeof(Type) });

        public bool CanMap(Type targetType, ColumnInfo column)
            => column.ColumnType.IsConvertible() && targetType.IsConvertible();

        public Expression Map(Type targetType, ColumnInfo column, ParameterExpression rawVar)
        {
            // raw != DBNull.Instance ? (targetType)Convert.ChangeType(raw, targetType) : default(targetType)
            var baseTargetType = targetType.GetTypeWithoutNullability();
            return Expression.Condition(
                Expression.NotEqual(Expressions._dbNullExp, rawVar),
                Expression.Convert(
                    Expression.Call(null, _convertMethod, new Expression[] { rawVar, Expression.Constant(baseTargetType, typeof(Type)) }), targetType),
                targetType.GetDefaultValueExpression());
        }
    }
}