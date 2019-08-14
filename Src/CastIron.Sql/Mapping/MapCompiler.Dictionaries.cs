using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping
{
    public partial class MapCompiler
    {
        // var dict = new MyCustomDictType();
        // dict.Add(convert(column)); ...
        private static ConstructedValueExpression GetConcreteDictionaryConversionExpression(MapCompileContext context, string name, Type targetType, Expression getExisting)
        {
            var idictType = targetType.GetInterfaces()
                .Where(i => i.IsGenericType)
                .FirstOrDefault(i => i.GetGenericTypeDefinition() == typeof(IDictionary<,>) && i.GenericTypeArguments[0] == typeof(string));
            if (idictType == null)
                throw MapCompilerException.InvalidDictionaryTargetType(targetType);
            var elementType = idictType.GetGenericArguments()[1];

            var constructor = targetType.GetConstructor(Type.EmptyTypes);
            if (constructor == null)
                throw MapCompilerException.InvalidDictionaryTargetType(targetType);

            var addMethod = idictType.GetMethod(nameof(IDictionary<string, object>.Add));
            if (addMethod == null)
                throw MapCompilerException.InvalidDictionaryTargetType(targetType);

            var expressions = new List<Expression>();
            var dictVar = GetMaybeInstantiateDictionaryExpression(context, targetType, getExisting, constructor, expressions);

            AddDictionaryPopulateStatements(context, name, elementType, dictVar, addMethod, expressions);

            return new ConstructedValueExpression(expressions, Expression.Convert(dictVar, targetType));
        }

        // IDictionary<string, T> dict = new Dictionary<string, T>()
        // dict.Add(convert(column)); ...
        // return dict as IDictionary<string, T> or IReadOnlyDictionary<string, T>
        private static ConstructedValueExpression GetDictionaryInterfaceConversionExpression(MapCompileContext context, string name, Type targetType, Expression getExisting)
        {
            var elementType = targetType.GetGenericArguments()[1];

            var dictType = typeof(Dictionary<,>).MakeGenericType(typeof(string), elementType);
            if (!targetType.IsAssignableFrom(dictType))
                throw MapCompilerException.CannotConvertType(targetType, dictType);

            var constructor = dictType.GetConstructor(Type.EmptyTypes);
            if (constructor == null)
                throw MapCompilerException.MissingParameterlessConstructor(dictType);

            var addMethod = dictType.GetMethod(nameof(Dictionary<string, object>.Add));
            if (addMethod == null)
                throw new Exception($".{nameof(Dictionary<string, object>.Add)}() Method missing from dictionary type {targetType.FullName}");

            var expressions = new List<Expression>();
            var dictVar = GetMaybeInstantiateDictionaryExpression(context, dictType, getExisting, constructor, expressions);

            AddDictionaryPopulateStatements(context, name, elementType, dictVar, addMethod, expressions);

            return new ConstructedValueExpression(expressions, targetType == dictType ? dictVar : Expression.Convert(dictVar, targetType));
        }

        // dict.Add(convert(column))
        private static void AddDictionaryPopulateStatements(MapCompileContext context, string name, Type elementType, Expression dictVar, MethodInfo addMethod, List<Expression> expressions)
        {
            if (name == null)
            {
                var firstColumnsByName = context.GetFirstIndexForEachColumnName();
                foreach (var column in firstColumnsByName)
                {
                    var getScalarExpression = GetConversionExpression(context, column.CanonicalName, elementType, null, null);
                    expressions.AddRange(getScalarExpression.Expressions);
                    expressions.Add(Expression.Call(dictVar, addMethod, Expression.Constant(column.OriginalName), getScalarExpression.FinalValue));
                }
                return;
            }

            var subcontext = context.CreateSubcontext(null, name);
            var columns = subcontext.GetFirstIndexForEachColumnName();
            foreach (var column in columns)
            {
                var keyName = column.OriginalName.Substring(name.Length + 1);
                var childName = column.CanonicalName.Substring(name.Length + 1);
                var getScalarExpression = GetConversionExpression(subcontext, childName, elementType, null, null);
                expressions.AddRange(getScalarExpression.Expressions);
                expressions.Add(Expression.Call(dictVar, addMethod, Expression.Constant(keyName), getScalarExpression.FinalValue));
            }
        }

        // var dict = new DictType();
        // OR
        // var dict = getExisting() == null ? new DictType() : getExisting() as DictType
        private static Expression GetMaybeInstantiateDictionaryExpression(MapCompileContext context, Type targetType, Expression getExisting, ConstructorInfo constructor, List<Expression> expressions)
        {
            var newInstance = context.AddVariable(targetType, "dict");
            if (getExisting == null)
            {
                // var newInstance = new TargetType()
                expressions.Add(Expression.Assign(newInstance, Expression.New(constructor)));
                return newInstance;
            }

            // var newInstance = getExisting() == null ? new TargetType() : getExisting()
            expressions.Add(Expression.Assign(
                newInstance,
                Expression.Condition(
                    Expression.Equal(
                        getExisting,
                        Expression.Constant(null)),
                    Expression.Convert(Expression.Assign(newInstance, Expression.New(constructor)), targetType),
                    Expression.Convert(getExisting, targetType))));
            return newInstance;
        }
    }
}
