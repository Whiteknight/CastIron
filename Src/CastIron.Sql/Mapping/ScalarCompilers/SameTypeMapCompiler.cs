﻿using System;
using System.Linq.Expressions;

namespace CastIron.Sql.Mapping.ScalarCompilers
{
    /// <summary>
    /// Compile an expression to copy a column value to the output, when the types are the same
    /// </summary>
    public class SameTypeMapCompiler : IScalarMapCompiler
    {
        // They are the same type, so we can directly assign them

        public bool CanMap(Type targetType, ColumnInfo column)
            => column.ColumnType == targetType || (!column.ColumnType.IsClass && typeof(Nullable<>).MakeGenericType(column.ColumnType) == targetType);

        public Expression Map(Type targetType, ColumnInfo column, ParameterExpression rawVar)
            =>
                // raw != DBNull.Instance ? (targetType)raw : default(targetType);
                Expression.Condition(
                    Expression.NotEqual(Expressions.DbNullExp, rawVar),
                    Expression.Convert(
                        rawVar,
                        targetType
                    ),
                    targetType.GetDefaultValueExpression()
                );
    }
}