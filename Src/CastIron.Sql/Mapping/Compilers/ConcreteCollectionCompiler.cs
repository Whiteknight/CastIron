using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace CastIron.Sql.Mapping.Compilers
{
    /// <summary>
    /// Compiles a mapping for any concrete implementation of ICollection
    /// </summary>
    public class ConcreteCollectionCompiler : ICompiler
    {
        private const string AddMethodName = nameof(ICollection<object>.Add);

        private readonly ICompiler _scalars;
        private readonly ICompiler _nonScalars;

        public ConcreteCollectionCompiler(ICompiler scalars, ICompiler nonScalars)
        {
            _scalars = scalars;
            _nonScalars = nonScalars;
        }

        public ConstructedValueExpression Compile(MapTypeContext context)
        {
            if (!IsSupportedType(context.TargetType))
                return ConstructedValueExpression.Nothing;

            // We have a concrete type which implements IEnumerable/IEnumerable<T> and has an Add 
            // method. Get these things.
            var (collectionType, elementType, constructor, addMethod) = GetTypeInfo(context);
            Debug.Assert(collectionType != null);
            Debug.Assert(context.TargetType.IsAssignableFrom(collectionType));
            Debug.Assert(elementType != null);
            Debug.Assert(addMethod != null);
            Debug.Assert(constructor != null);

            var collectionVar = CollectionExpressionFactory.GetMaybeInstantiateCollectionExpression(context, constructor);
            var addStmts = CollectionExpressionFactory.GetCollectionPopulateStatements(_scalars, _nonScalars, context, elementType, collectionVar.FinalValue, addMethod);

            return new ConstructedValueExpression(
                collectionVar.Expressions.Concat(addStmts.Expressions),
                collectionVar.FinalValue,
                collectionVar.Variables.Concat(addStmts.Variables));
        }

        private static bool IsSupportedType(Type t)
        {
            if (t.IsInterface || t.IsAbstract)
                return false;

            if (!t.GetInterfaces().Any(i => i == typeof(IEnumerable) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))))
                return false;
            var addMethod = t.GetMethod("Add", BindingFlags.Public | BindingFlags.Instance);
            return addMethod != null;
        }

        private ConcreteCollectionInfo GetTypeInfo(MapTypeContext context)
        {
            var targetType = context.TargetType;
            var typeSettings = context.Settings.GetTypeSettings(targetType);
            if (typeSettings.BaseType != typeof(object))
                targetType = typeSettings.GetDefault().Type;

            // We absolutely need a constructor here, there's no way around it. The user has asked
            // for a specific concrete type, we can either return that type or the configured 
            // subclass, but we can't default to something else with a parameterless constructor
            var constructor = targetType.GetConstructor(Type.EmptyTypes);
            if (constructor == null)
                throw MapCompilerException.InvalidCollectionType(targetType);

            // Same with .Add(). The type absolutely must have a .Add(T) method variant or we
            // cannot proceed
            var addMethods = targetType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name == AddMethodName && m.GetParameters().Length == 1)
                .ToList();
            if (addMethods.Count == 0)
                throw MapCompilerException.InvalidCollectionType(targetType);

            // Find all IEnumerable<T> interfaces. For each one we're going to look for a matching
            // .Add(T) method. If we find a match, that's what we're going with.
            var iEnumerableOfTTypes = targetType
                .GetInterfaces()
                .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                .ToDictionary(t => t.GetGenericArguments()[0]);
            if (iEnumerableOfTTypes.Count > 0)
            {
                var bestAddMethod = addMethods.FirstOrDefault(m => iEnumerableOfTTypes.ContainsKey(m.GetParameters()[0].ParameterType));
                if (bestAddMethod != null)
                {
                    var bestAddElementType = bestAddMethod.GetParameters()[0].ParameterType;
                    return new ConcreteCollectionInfo(targetType, bestAddElementType, constructor, bestAddMethod);
                }
            }

            // At this point we know the collection must be IEnumerable because of precondition
            // checks. So we'll just use any available Add method 
            var addMethod = addMethods.FirstOrDefault();
            var elementType = addMethod.GetParameters()[0].ParameterType;
            return new ConcreteCollectionInfo(targetType, elementType, constructor, addMethod);
        }
    }
}
