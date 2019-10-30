using System;
using System.Collections;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CastIron.Sql.Utility;

namespace CastIron.Sql.Mapping
{
    public partial class MapCompiler : IMapCompiler
    {
        // TODO: Ability to take the IDataRecord in the factory and consume some columns for constructor params so they aren't used later for properties?
        public Func<IDataRecord, T> CompileExpression<T>(MapCompileContext context, IDataReader reader)
        {
            Argument.NotNull(context, nameof(context));

            var targetType = context.Specific;

            // For all other cases, fall back to normal recursive conversion routines.
            var expressions = GetConversionExpression(context, null, targetType, null, null);
            var statements = expressions.Expressions.Concat(new[] { expressions.FinalValue }).Where(e => e != null);

            var lambdaExpression = Expression.Lambda<Func<IDataRecord, T>>(
                Expression.Block(typeof(T), context.Variables, statements), context.RecordParam
            );
            DumpCodeToDebugConsole(lambdaExpression);
            return lambdaExpression.Compile();
        }

        // Helper method to get a reasonably complete code listing, to help with debugging
        [Conditional("DEBUG")]
        private static void DumpCodeToDebugConsole(Expression expr)
        {
            // This property is marked private, so we can only get to it by reflection, which would be terrible if
            // this wasn't a debug-only helper routine
            var debugViewProp = typeof(Expression).GetProperty("DebugView", BindingFlags.Instance | BindingFlags.NonPublic);
            if (debugViewProp == null)
                return;
            var code = (string)debugViewProp.GetGetMethod(true).Invoke(expr, null);
            Debug.WriteLine(code);
        }

        private static ConstructedValueExpression GetConversionExpression(MapCompileContext context, string name, Type targetType, Expression existing, ICustomAttributeProvider attrs)
        {
            if (targetType == typeof(object))
                return GetObjectConversionExpression(context, name, targetType, attrs);
            if (targetType.IsSupportedPrimitiveType())
                return GetPrimitiveConversionExpression(context, name, targetType);

            if (targetType.IsArray && targetType.HasElementType)
                return GetArrayConversionExpression(context, name, targetType, attrs);
            if (targetType == typeof(IEnumerable) || targetType == typeof(IList) || targetType == typeof(ICollection))
                return GetArrayConversionExpression(context, name, typeof(object[]), attrs);

            if (targetType.IsConcreteDictionaryType())
                return GetConcreteDictionaryConversionExpression(context, name, targetType, existing);
            if (targetType.IsDictionaryInterfaceType())
                return GetDictionaryInterfaceConversionExpression(context, name, targetType, existing);

            if (targetType.IsGenericType && targetType.IsInterface && (targetType.Namespace ?? string.Empty).StartsWith("System.Collections"))
                return GetInterfaceCollectionConversionExpression(context, name, targetType, existing, attrs);

            if (targetType.IsConcreteCollectionType())
                return GetConcreteCollectionConversionExpression(context, name, targetType, existing, attrs);

            if (targetType.IsTupleType())
                return GetTupleConversionExpression(context, name, targetType);

            if (IsMappableCustomObjectType(targetType))
                return GetCustomObjectConversionExpression(context, name, targetType);

            return null;
        }

        private static ConstructedValueExpression GetPrimitiveConversionExpression(MapCompileContext context, string name, Type targetType)
        {
            var firstColumn = context.Columns.GetColumn(name);
            return firstColumn == null ? ConstructedValueExpression.Nothing() : GetScalarMappingExpression(context, firstColumn, targetType);
        }
    }
}