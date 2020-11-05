using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping.Compilers
{
    /// <summary>
    /// Maps to any dictionary type which inherits string-keyed IDictionary
    /// </summary>
    public class ConcreteDictionaryCompiler : ICompiler
    {
        private readonly ICompiler _valueCompiler;

        public ConcreteDictionaryCompiler(ICompiler valueCompiler)
        {
            _valueCompiler = valueCompiler;
        }

        public ConstructedValueExpression Compile(MapTypeContext context)
        {
            if (!IsSupportedType(context.TargetType))
                return ConstructedValueExpression.Nothing;

            var idictType = GetIDictionaryType(context);
            var elementType = idictType.GetGenericArguments()[1];

            var constructor = GetDictionaryConstructor(context.TargetType);
            var addMethod = GetIDictionaryAddMethod(context.TargetType, idictType);

            var dictVar = DictionaryExpressionFactory.GetMaybeInstantiateDictionaryExpression(context, constructor);
            var addStmts = DictionaryExpressionFactory.AddDictionaryPopulateStatements(_valueCompiler, context, elementType, dictVar.FinalValue, addMethod);

            return new ConstructedValueExpression(
                dictVar.Expressions.Concat(addStmts.Expressions),
                Expression.Convert(dictVar.FinalValue, context.TargetType),
                dictVar.Variables.Concat(addStmts.Variables));
        }

        // It is Dictionary<string,X> or is concrete and inherits from IDictionary<string,X>
        private static bool IsSupportedType(Type t)
        {
            if (t.IsInterface || t.IsAbstract)
                return false;
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>) && t.GenericTypeArguments[0] == typeof(string))
                return true;
            var dictType = t.GetInterfaces()
                .Where(i => i.IsGenericType)
                .FirstOrDefault(i => i.GetGenericTypeDefinition() == typeof(IDictionary<,>) && i.GenericTypeArguments[0] == typeof(string));
            return dictType != null;
        }

        private static MethodInfo GetIDictionaryAddMethod(Type targetType, Type idictType)
        {
            return idictType.GetMethod(nameof(IDictionary<string, object>.Add)) ?? throw MapCompilerException.InvalidDictionaryTargetType(targetType);
        }

        private static ConstructorInfo GetDictionaryConstructor(Type targetType)
        {
            return targetType.GetConstructor(Type.EmptyTypes) ?? throw MapCompilerException.InvalidDictionaryTargetType(targetType);
        }

        private static Type GetIDictionaryType(MapTypeContext context)
        {
            return context.TargetType.GetInterfaces()
                .Where(i => i.IsGenericType)
                .FirstOrDefault(i => i.GetGenericTypeDefinition() == typeof(IDictionary<,>) && i.GenericTypeArguments[0] == typeof(string))
                ?? throw MapCompilerException.InvalidDictionaryTargetType(context.TargetType);
        }
    }
}
