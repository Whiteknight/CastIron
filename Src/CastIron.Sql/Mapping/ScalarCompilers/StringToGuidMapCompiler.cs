using System;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping.ScalarCompilers
{
    /// <summary>
    /// Compile an expression to convert a string column value to Guid
    /// </summary>
    public class StringToGuidMapCompiler : IScalarMapCompiler
    {
        private static readonly MethodInfo _guidParseMethod = typeof(Guid).GetMethod(nameof(Guid.Parse), new[] { typeof(string) });

        public bool CanMap(Type targetType, Type columnType, string sqlTypeName)
            => columnType == typeof(string) && (targetType == typeof(Guid) || targetType == typeof(Guid?));

        public Expression Map(Type targetType, Type columnType, string sqlTypeName, ParameterExpression rawVar)
            =>
                // Create expression tree to represent "result = raw != DBNull ? (targetType)Guid.Parse((string)raw) : default(Guid);"
                Expression.Condition(
                    Expression.NotEqual(Expressions.DbNullExp, rawVar),
                    Expression.Convert(
                        Expression.Call(
                            null,
                            _guidParseMethod,
                            Expression.Convert(rawVar, typeof(string))
                        ),
                        targetType
                    ),
                    targetType.GetDefaultValueExpression()
                );
    }
}