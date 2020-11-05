using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping.Compilers
{
    /// <summary>
    /// Maps abstract types IDictionary and IReadOnlyDictionary to a concrete Dictionary class with string
    /// key
    /// </summary>
    public class AbstractDictionaryCompiler : ICompiler
    {
        private const string AddMethodName = nameof(IDictionary<string, object>.Add);

        private readonly ICompiler _valueCompiler;

        public AbstractDictionaryCompiler(ICompiler valueCompiler)
        {
            _valueCompiler = valueCompiler;
        }

        private struct ConcreteTypeInfo
        {
            public ConcreteTypeInfo(Type concreteType, ConstructorInfo constructor, MethodInfo addMethod)
            {
                ConcreteType = concreteType;
                Constructor = constructor;
                AddMethod = addMethod;
            }

            public Type ConcreteType { get; }
            public ConstructorInfo Constructor { get; }
            public MethodInfo AddMethod { get; }
        }

        public ConstructedValueExpression Compile(MapTypeContext context)
        {
            if (!IsSupportedType(context.TargetType))
                return ConstructedValueExpression.Nothing;

            var genericArguments = context.TargetType.GetGenericArguments();
            Debug.Assert(genericArguments.Length == 2);

            var keyType = typeof(string);
            Debug.Assert(genericArguments[0] == typeof(string));

            var elementType = context.TargetType.GetGenericArguments()[1];
            Debug.Assert(elementType != null);

            var typeInfo = GetConcreteTypeInfo(context, keyType, elementType);
            var dictType = typeInfo.ConcreteType;
            Debug.Assert(dictType != null);

            var constructor = typeInfo.Constructor;
            Debug.Assert(constructor != null);

            var addMethod = typeInfo.AddMethod;
            Debug.Assert(addMethod != null);

            var concreteState = context.ChangeTargetType(dictType);
            var dictVar = DictionaryExpressionFactory.GetMaybeInstantiateDictionaryExpression(concreteState, constructor);
            var addStmts = DictionaryExpressionFactory.AddDictionaryPopulateStatements(_valueCompiler, concreteState, elementType, dictVar.FinalValue, addMethod);

            return new ConstructedValueExpression(
                dictVar.Expressions.Concat(addStmts.Expressions),
                Expression.Convert(dictVar.FinalValue, context.TargetType),
                dictVar.Variables.Concat(addStmts.Variables)
            );
        }

        // Is one of IDictionary<string,X> or IReadOnlyDictionary<string,X>
        private static bool IsSupportedType(Type t)
        {
            if (!t.IsGenericType || !t.IsInterface)
                return false;
            var genericDef = t.GetGenericTypeDefinition();
            return (genericDef == typeof(IDictionary<,>) || genericDef == typeof(IReadOnlyDictionary<,>))
                   && t.GenericTypeArguments[0] == typeof(string);
        }

        private ConcreteTypeInfo GetConcreteTypeInfo(MapTypeContext context, Type keyType, Type elementType)
        {
            // If we have a custom type try to use that. If we don't have one, or it fails any
            // of the precondition checks, fall back to Dictionary<string, T>
            var typeSettings = context.Settings.GetTypeSettings(context.TargetType);
            if (typeSettings.BaseType != typeof(object))
            {
                var customType = typeSettings.GetDefault().Type;
                var constructor = customType.GetConstructor(Type.EmptyTypes);
                var addMethod = customType.GetMethod(AddMethodName, BindingFlags.Public | BindingFlags.Instance, null, new[] { keyType, elementType }, null);
                if (constructor == null || addMethod == null)
                    return GetDictionaryOfTTypeInfo(keyType, elementType);
                return new ConcreteTypeInfo(customType, constructor, addMethod);
            }
            return GetDictionaryOfTTypeInfo(keyType, elementType);
        }

        private ConcreteTypeInfo GetDictionaryOfTTypeInfo(Type keyType, Type elementType)
        {
            var dictType = typeof(Dictionary<,>).MakeGenericType(typeof(string), elementType);
            var constructor = dictType.GetConstructor(Type.EmptyTypes);
            var addMethod = dictType.GetMethod(AddMethodName, new[] { keyType, elementType });
            return new ConcreteTypeInfo(dictType, constructor, addMethod);
        }
    }
}