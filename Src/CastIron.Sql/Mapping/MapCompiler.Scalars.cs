using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using CastIron.Sql.Mapping.Scalars;

namespace CastIron.Sql.Mapping
{
    public partial class MapCompiler
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

        public static ConstructedValueExpression GetScalarMappingExpression(MapCompileContext context, ColumnInfo column, Type targetType)
        {
            var expressions = new List<Expression>();

            // Pull the value out of the reader into the rawVar
            var rawVar = context.AddVariable<object>("raw");
            var getRawStmt = Expression.Assign(rawVar, Expression.Call(context.RecordParam, _getValueMethod, Expression.Constant(column.Index)));
            expressions.Add(getRawStmt);

            var rawConvertExpr = MapScalar(targetType, column, rawVar);
            column.MarkMapped();
            return new ConstructedValueExpression(expressions, rawConvertExpr);
        }

        private static Expression MapScalar(Type targetType, ColumnInfo column, ParameterExpression rawVar)
        {
            for (var i = _scalarMapCompilers.Count - 1; i >= 0; i--)
            {
                if (_scalarMapCompilers[i].CanMap(targetType, column))
                    return _scalarMapCompilers[i].Map(targetType, column, rawVar);
            }

            return targetType.GetDefaultValueExpression();
        }
    }
}
