using System;
using System.Linq.Expressions;

namespace CastIron.Sql.Mapping.ScalarCompilers
{
    /// <summary>
    /// Compile an expression to convert a column value to string
    /// </summary>
    public class ToStringMapCompiler : IScalarMapCompiler
    {
        public bool CanMap(Type targetType, ColumnInfo column) => targetType == typeof(string);

        public Expression Map(Type targetType, ColumnInfo column, ParameterExpression rawVar)
            =>
                // result = raw != DBNull ? raw.ToString() : default(string);
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