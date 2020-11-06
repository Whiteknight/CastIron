using System;
using System.Linq.Expressions;

namespace CastIron.Sql.Mapping.ScalarCompilers
{
    /// <summary>
    /// Compile an expression to convert a column value to string
    /// </summary>
    public class ToStringMapCompiler : IScalarMapCompiler
    {
        public bool CanMap(Type targetType, Type columnType, string sqlTypeName)
            => targetType == typeof(string);

        public Expression Map(Type targetType, Type columnType, string sqlTypeName, ParameterExpression rawVar)
            =>
                // Create expression tree to represent "result = raw != DBNull ? raw.ToString() : default(string);"
                Expression.Condition(
                    Expression.NotEqual(Expressions.DbNullExp, rawVar),
                    Expression.Call(
                        rawVar,
                        nameof(ToString),
                        Type.EmptyTypes
                    ),
                    typeof(string).GetDefaultValueExpression()
                );
    }
}