using System;
using System.Linq.Expressions;

namespace CastIron.Sql.Mapping.ScalarCompilers
{
    /// <summary>
    /// Compile an expression to copy a column value to an "object" regardless of type
    /// </summary>
    public class ObjectMapCompiler : IScalarMapCompiler
    {
        public bool CanMap(Type targetType, Type columnType, string sqlTypeName) 
            => targetType == typeof(object);

        public Expression Map(Type targetType, Type columnType, string sqlTypeName, ParameterExpression rawVar)
            =>
                // result = raw != DBNull ? raw : default(object);
                Expression.Condition(
                    Expression.NotEqual(Expressions.DbNullExp, rawVar),
                    rawVar,
                    typeof(object).GetDefaultValueExpression()
                );
    }
}