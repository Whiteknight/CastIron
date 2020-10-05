using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping.Compilers
{
    /// <summary>
    /// Compiler type to map a column to one of the supported primitive types
    /// </summary>
    public class ScalarCompiler : ICompiler
    {
        private readonly IReadOnlyList<IScalarMapCompiler> _scalarCompilers;
        private static readonly MethodInfo _getValueMethod = typeof(IDataRecord).GetMethod(nameof(IDataRecord.GetValue), new[] { typeof(int) });

        public ScalarCompiler(IEnumerable<IScalarMapCompiler> scalarCompilers)
        {
            _scalarCompilers = scalarCompilers.ToList();
            if (_scalarCompilers.Count == 0)
                throw MapCompilerException.NoScalarMapCompilers();
        }

        public ConstructedValueExpression Compile(MapTypeContext context)
        {
            if (!context.TargetType.IsSupportedPrimitiveType() && context.TargetType != typeof(object))
                return ConstructedValueExpression.Nothing;

            var column = context.SingleColumn();
            if (column == null)
                return ConstructedValueExpression.Nothing;

            // Pull the value out of the reader into the rawVar
            var rawVar = context.CreateVariable(typeof(object), "raw");
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

        private Expression MapScalar(Type targetType, ColumnInfo column, ParameterExpression rawVar)
        {
            // I put the compilers in reverse order, because I think when we can add custom compilers, they
            // will be added at the end of the list (so they will run first before falling back to our
            // other types and finally our default compiler
            foreach (var compiler in _scalarCompilers)
            {
                if (compiler.CanMap(targetType, column.ColumnType, column.SqlTypeName))
                    return compiler.Map(targetType, column.ColumnType, column.SqlTypeName, rawVar);
            }

            return targetType.GetDefaultValueExpression();
        }
    }
}