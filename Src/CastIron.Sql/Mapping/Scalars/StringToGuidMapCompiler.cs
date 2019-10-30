using System;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping.Scalars
{
    public class StringToGuidMapCompiler : IScalarMapCompiler
    {
        private static readonly MethodInfo _guidParseMethod = typeof(Guid).GetMethod(nameof(Guid.Parse), new[] { typeof(string) });

        public bool CanMap(Type targetType, ColumnInfo column)
            => column.ColumnType == typeof(string) && (targetType == typeof(Guid) || targetType == typeof(Guid?));

        public Expression Map(Type targetType, ColumnInfo column, ParameterExpression rawVar)
            => Expression.Condition(
                Expression.NotEqual(Expressions._dbNullExp, rawVar),
                Expression.Convert(
                    Expression.Call(null, _guidParseMethod, Expression.Convert(rawVar, typeof(string))), targetType),
                targetType.GetDefaultValueExpression());
    }
}