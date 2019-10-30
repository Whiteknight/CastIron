using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping
{
    public partial class MapCompiler
    {
        private static ConstructedValueExpression GetTupleConversionExpression(MapCompileContext context, string name, Type targetType)
        {
            var columns = context.Columns.GetColumns(name).ToList();
            return GetTupleConversionExpression(context, targetType, columns);
        }

        // value = Tuple.Create(converted columns)
        private static ConstructedValueExpression GetTupleConversionExpression(MapCompileContext context, Type targetType, IReadOnlyList<ColumnInfo> columns)
        {
            var typeParams = targetType.GenericTypeArguments;
            if (typeParams.Length == 0 || typeParams.Length > 7)
                throw MapCompilerException.InvalidTupleMap(typeParams.Length, targetType);

            var factoryMethod = typeof(Tuple).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == nameof(Tuple.Create) && m.GetParameters().Length == typeParams.Length)
                .Select(m => m.MakeGenericMethod(typeParams))
                .FirstOrDefault();
            if (factoryMethod == null)
                throw MapCompilerException.InvalidTupleMap(typeParams.Length, targetType);

            var args = new Expression[typeParams.Length];
            var expressions = new List<Expression>();

            for (var i = 0; i < typeParams.Length; i++)
                args[i] = GetTupleArgumentConversionExpression(context, i, columns, typeParams[i], expressions);
            return new ConstructedValueExpression(expressions, Expression.Call(null, factoryMethod, args));
        }

        // argumentValue[i] = Convert(columns[i])
        private static Expression GetTupleArgumentConversionExpression(MapCompileContext context, int i, IReadOnlyList<ColumnInfo> columns, Type parameterType, List<Expression> expressions)
        {
            if (i >= columns.Count)
                return parameterType.GetDefaultValueExpression();

            var column = columns[i];
            var expr = GetScalarMappingExpression(context, column, parameterType);
            expressions.AddRange(expr.Expressions);
            return expr.FinalValue;
        }
    }
}
