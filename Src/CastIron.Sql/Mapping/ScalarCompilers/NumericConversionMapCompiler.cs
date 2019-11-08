﻿using System;
using System.Linq.Expressions;

namespace CastIron.Sql.Mapping.ScalarCompilers
{
    /// <summary>
    /// Compile an expression to convert a numeric column to a numeric value, where the types are not the
    /// same. The source value is unboxed, cast to the appropriate type, and then boxed again.
    /// </summary>
    public class NumericConversionMapCompiler : IScalarMapCompiler
    {
        // They are both numeric but not the same type. Unbox and convert

        public bool CanMap(Type targetType, ColumnInfo column)
            => column.ColumnType.IsNumericType() && targetType.IsNumericType();

        public Expression Map(Type targetType, ColumnInfo column, ParameterExpression rawVar)
            =>
                // result = raw != DBNull ? (targetType)unbox(raw) : default(targetType
                Expression.Condition(
                    Expression.NotEqual(Expressions.DbNullExp, rawVar),
                    Expression.Convert(
                        Expression.Unbox(rawVar, column.ColumnType),
                        targetType
                    ),
                    targetType.GetDefaultValueExpression()
                );
    }
}