using System;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping.ScalarCompilers
{
    /// <summary>
    /// Compile an expression to use the IConvertable interface to convert the column value to the target
    /// type. Requires both the column value and the target type inherit from IConvertible
    /// </summary>
    public class ConvertibleMapCompiler : IScalarMapCompiler
    {
        private static readonly MethodInfo _convertMethod = typeof(Convert).GetMethod(nameof(Convert.ChangeType), new[] { typeof(object), typeof(Type) });

        public bool CanMap(Type targetType, Type columnType, string sqlTypeName)
            => columnType.IsConvertible() && targetType.IsConvertible();

        public Expression Map(Type targetType, Type columnType, string sqlTypeName, ParameterExpression rawVar)
        {
            // raw != DBNull.Instance ? (targetType)Convert.ChangeType(raw, targetType) : default(targetType)
            var baseTargetType = targetType.GetTypeWithoutNullability();
            return Expression.Condition(
                Expression.NotEqual(Expressions.DbNullExp, rawVar),
                Expression.Convert(
                    Expression.Call(
                        null, 
                        _convertMethod,
                        new Expression[] { 
                            rawVar, 
                            Expression.Constant(baseTargetType, typeof(Type)) 
                        }
                    ), 
                    targetType
                ),
                targetType.GetDefaultValueExpression()
            );
        }
    }
}