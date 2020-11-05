using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping.Compilers
{
    /// <summary>
    /// Attempt to instantiate a collection to fill a slot of type IList, IReadOnlyList, 
    /// ICollection, IEnumerable, etc
    /// </summary>
    public class AbstractCollectionCompiler : ICompiler
    {
        private readonly ICompiler _scalars;
        private readonly ICompiler _nonScalars;

        private static readonly HashSet<Type> _nonGenericCollectionInterfaces = new HashSet<Type>
        {
            typeof(IEnumerable),
            typeof(ICollection),
            typeof(IList)
        };

        private static readonly HashSet<Type> _genericCollectionBaseTypes = new HashSet<Type>
        {
            typeof(IEnumerable<>),
            typeof(ICollection<>),
            typeof(IList<>),
            typeof(IReadOnlyCollection<>),
            typeof(IReadOnlyList<>),
            typeof(ISet<>)
        };

        public AbstractCollectionCompiler(ICompiler scalars, ICompiler nonScalars)
        {
            _scalars = scalars;
            _nonScalars = nonScalars;
        }

        public ConstructedValueExpression Compile(MapTypeContext context)
        {
            if (!IsSupportedType(context.TargetType))
                return ConstructedValueExpression.Nothing;

            // Get the concrete type to construct, the element type to populate, and the Add
            // method to call for each value
            var (collectionType, elementType, constructor, addMethod) = GetConcreteTypeInfo(context);
            Debug.Assert(collectionType != null);
            Debug.Assert(context.TargetType.IsAssignableFrom(collectionType));
            Debug.Assert(elementType != null);
            Debug.Assert(addMethod != null);
            Debug.Assert(constructor != null);

            var originalTargetType = context.TargetType;
            context = context.ChangeTargetType(collectionType);

            // Get a list of expressions to create the collection instance and populate it with
            // values
            var collectionVar = CollectionExpressionFactory.GetMaybeInstantiateCollectionExpression(context, constructor);
            var addStmts = CollectionExpressionFactory.GetCollectionPopulateStatements(_scalars, _nonScalars, context, elementType, collectionVar.FinalValue, addMethod);

            return new ConstructedValueExpression(
                collectionVar.Expressions.Concat(addStmts.Expressions),
                Expression.Convert(collectionVar.FinalValue, originalTargetType),
                collectionVar.Variables.Concat(addStmts.Variables)
            );
        }

        private static bool IsSupportedType(Type t)
        {
            if (!t.IsInterface)
                return false;
            if (_nonGenericCollectionInterfaces.Contains(t))
                return true;
            if (!t.IsGenericType)
                return false;
            return _genericCollectionBaseTypes.Contains(t.GetGenericTypeDefinition());
        }

        private ConcreteCollectionInfo GetConcreteTypeInfo(MapTypeContext context)
        {
            // By the time we get into this Compiler we have already limited the valid TargetType
            // values which are possible. At this point we either have a non-generic collection 
            // interface (IEnumerable, ICollection, IList) or we have a generic collection
            // interface type (IEnumerable<T>, ICollection<T>, IReadOnlyCollection<T>, IList<T>
            // or IReadOnlyList<T>). Separate out because we have different strategies for each
            if (context.TargetType.IsGenericType && context.TargetType.GenericTypeArguments.Length > 0)
                return GetConcreteTypeInfoForGenericInterfaceType(context);

            return GetConcreteTypeInfoForInterfaceType(context);
        }

        private ConcreteCollectionInfo GetConcreteTypeInfoForGenericInterfaceType(MapTypeContext context)
        {
            // We have one of the interfaces IEnumerable<T>, ICollection<T>, 
            // IReadOnlyCollection<T>, IList<T> or IReadOnlyList<T>, get T from generic type
            // definition
            var elementType = context.TargetType.GenericTypeArguments[0];

            Type collectionType = null;

            // See if we have a custom type configured which we should use instead of List<T>
            var typeSettings = context.Settings.GetTypeSettings(context.TargetType);
            if (typeSettings.BaseType != null && typeSettings.BaseType != typeof(object))
                collectionType = typeSettings.GetDefault().Type;

            // Just use List<T> if there is no custom type
            if (collectionType == null)
                return GetDefaultConcreteTypeInfo(context.TargetType, elementType);

            // The type should have an Add(T) method (even if the interface we're using is 
            // IReadOnly...<T>). The Add method might not be exposed through the interface.
            var addMethod = collectionType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == nameof(ICollection<object>.Add) && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == elementType);
            var constructor = collectionType.GetConstructor(new Type[0]);

            // Fall back to List<T> if the custom type isn't working out.
            if (addMethod == null || constructor == null)
                return GetDefaultConcreteTypeInfo(context.TargetType, elementType);

            return new ConcreteCollectionInfo(collectionType, elementType, constructor, addMethod);
        }

        private ConcreteCollectionInfo GetConcreteTypeInfoForInterfaceType(MapTypeContext context)
        {
            // It's one of IEnumerable, ICollection or IList, so we will do some searching to find
            // an appropriate elementType and Add() method variant
            var typeSettings = context.Settings.GetTypeSettings(context.TargetType);
            if (typeSettings.BaseType == typeof(object))
                return GetDefaultConcreteTypeInfo(context.TargetType, typeof(object));

            var customType = typeSettings.GetDefault().Type;

            var constructor = customType.GetConstructor(new Type[0]);
            // The custom type might have multiple .Add() method variants. We have to try to pick
            // the "Best" one, which is hard to do. Basically we want Add(<anyType>) instead of 
            // Add(object), if possible.
            var addMethods = customType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name == nameof(ICollection<object>.Add) && m.GetParameters().Length == 1)
                .Select(m => new { AddMethod = m, Parameter = m.GetParameters()[0] })
                .OrderBy(x => x.Parameter.ParameterType == typeof(object) ? 0 : 1)
                .ToList();

            // If we don't have a default constructor or a suitable .Add() method, fall back to
            // List<object>. There's no way to signal an error from here, so the user has to 
            // figure it out.
            if (constructor == null || addMethods.Count == 0)
                return GetDefaultConcreteTypeInfo(context.TargetType, typeof(object));

            var bestAddMethod = addMethods.FirstOrDefault();
            return new ConcreteCollectionInfo(customType, bestAddMethod.Parameter.ParameterType, constructor, bestAddMethod.AddMethod);
        }

        private ConcreteCollectionInfo GetDefaultConcreteTypeInfo(Type targetType, Type elementType)
        {
            // Return List<T> for everything except ISet<T>. Return SortedSet<T> if we are looking for ISet<T>
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(ISet<>))
                return GetConcreteTypeInfo(typeof(SortedSet<>).MakeGenericType(elementType), elementType);
            return GetConcreteTypeInfo(typeof(List<>).MakeGenericType(elementType), elementType);
        }

        private ConcreteCollectionInfo GetConcreteTypeInfo(Type concreteType, Type elementType)
        {
            var addTMethod = concreteType.GetMethod(nameof(List<object>.Add), BindingFlags.Public | BindingFlags.Instance);
            var listTConstructor = concreteType.GetConstructor(new Type[0]);
            return new ConcreteCollectionInfo(concreteType, elementType, listTConstructor, addTMethod);
        }
    }
}