using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using CastIron.Sql.Mapping.ScalarCompilers;

namespace CastIron.Sql.Mapping.Compilers
{
    /// <summary>
    /// Compiler type to map a column to one of the supported primitive types
    /// </summary>
    public class ScalarCompiler : ICompiler
    {
        private static readonly MethodInfo _getValueMethod = typeof(IDataRecord).GetMethod(nameof(IDataRecord.GetValue), new[] { typeof(int) });

        // TODO: If the column is a serialized type like XML or JSON, we should be able to deserialize that into an object.
        // TODO: Where to put this list so that it's configurable by the user?
        private static readonly IReadOnlyList<IScalarMapCompiler> _scalarMapCompilers = new List<IScalarMapCompiler>
        {
            new DefaultScalarMapCompiler(),
            new ConvertibleMapCompiler(),
            new StringToGuidMapCompiler(),
            new NumberToBoolMapCompiler(),
            new NumericConversionMapCompiler(),
            new ToStringMapCompiler(),
            new ObjectMapCompiler(),
            new SameTypeMapCompiler()
        };

        public ConstructedValueExpression Compile(MapContext context)
        {
            var column = context.SingleColumn();
            if (column == null)
                return ConstructedValueExpression.Nothing;

            // Pull the value out of the reader into the rawVar
            var rawVar = context.CreateVariable<object>("raw");
            var getRawStmt = Expression.Assign(
                rawVar,
                Expression.Call(
                    context.RecordParameter,
                    _getValueMethod,
                    Expression.Constant(column.Index)
                )
            );

            var rawConvertExpr = MapScalar(context.TargetType, column, rawVar);
            column.MarkMapped();
            return new ConstructedValueExpression(new[] { getRawStmt }, rawConvertExpr, new[] { rawVar });
        }

        private static Expression MapScalar(Type targetType, ColumnInfo column, ParameterExpression rawVar)
        {
            // I put the compilers in reverse order, because I think when we can add custom compilers, they
            // will be added at the end of the list (so they will run first before falling back to our
            // other types and finally our default compiler
            for (var i = _scalarMapCompilers.Count - 1; i >= 0; i--)
            {
                if (_scalarMapCompilers[i].CanMap(targetType, column.ColumnType, column.SqlTypeName))
                    return _scalarMapCompilers[i].Map(targetType, column.ColumnType, column.SqlTypeName, rawVar);
            }

            return targetType.GetDefaultValueExpression();
        }
    }
}