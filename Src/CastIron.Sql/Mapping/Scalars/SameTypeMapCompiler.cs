using System;
using System.Linq.Expressions;

namespace CastIron.Sql.Mapping.Scalars
{
    public class SameTypeMapCompiler : IScalarMapCompiler
    {
        // They are the same type, so we can directly assign them

        public bool CanMap(Type targetType, ColumnInfo column)
            => column.ColumnType == targetType || (!column.ColumnType.IsClass && typeof(Nullable<>).MakeGenericType(column.ColumnType) == targetType);

        public Expression Map(Type targetType, ColumnInfo column, ParameterExpression rawVar)
            // raw != DBNull.Instance ? (targetType)raw : default(targetType);
            => Expression.Condition(
                Expression.NotEqual(Expressions._dbNullExp, rawVar),
                Expression.Convert(
                    rawVar,
                    targetType
                ),
                targetType.GetDefaultValueExpression());
    }
}