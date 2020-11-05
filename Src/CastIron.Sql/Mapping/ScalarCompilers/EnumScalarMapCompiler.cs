using System;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping.ScalarCompilers
{
    public class EnumScalarMapCompiler : IScalarMapCompiler
    {
        private static readonly MethodInfo _enumParseMethod = typeof(Enum).GetMethod(nameof(Enum.Parse), new Type[] { typeof(Type), typeof(string), typeof(bool) });

        public bool CanMap(Type targetType, Type columnType, string sqlTypeName)
            => targetType.IsEnum && (columnType == typeof(string) || columnType == typeof(byte) || columnType == typeof(int) || columnType == typeof(long));

        public Expression Map(Type targetType, Type columnType, string sqlTypeName, ParameterExpression rawVar)
        {
            if (columnType == typeof(string))
            {
                return Expression.Convert(
                    Expression.Call(
                        null,
                        _enumParseMethod,
                        Expression.Constant(targetType),
                        Expression.Convert(rawVar, typeof(string)),
                        Expression.Constant(true)
                    ),
                    targetType
                );
            }
            if (columnType == typeof(byte) || columnType == typeof(int) || columnType == typeof(long))
            {
                // Unbox object -> columnType, cast columnType -> int, cast int -> enum
                return Expression.Convert(
                    Expression.Convert(
                        Expression.Convert(rawVar, columnType),
                        typeof(int)
                    ),
                    targetType
                );
            }
            throw new MapCompilerException("Unsupported source type for enum conversion");
        }
    }
}
