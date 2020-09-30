using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace CastIron.Sql.Mapping
{
    public static class TypeExtensions
    {
        public static string GetFriendlyName(this Type t)
            => TypeFriendlyNameStringifier.Instance.GetFriendlyName(t);

        public static bool IsConvertible(this Type t)
            => t != null && typeof(IConvertible).IsAssignableFrom(t);

        public static Expression GetDefaultValueExpression(this Type t)
            => Expression.Convert(Expression.Constant(t.GetDefaultValue()), t);

        public static Type GetTypeWithoutNullability(this Type t)
            => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>) ? t.GenericTypeArguments[0] : t;

        private static readonly HashSet<Type> _tupleTypes = new HashSet<Type>
        {
            typeof(Tuple<>),
            typeof(Tuple<,>),
            typeof(Tuple<,,>),
            typeof(Tuple<,,,>),
            typeof(Tuple<,,,,>),
            typeof(Tuple<,,,,,>),
            typeof(Tuple<,,,,,,>)
        };

        public static bool IsTupleType(this Type parentType)
        {
            if (!parentType.IsGenericType || !parentType.IsConstructedGenericType)
                return false;
            var genericTypeDef = parentType.GetGenericTypeDefinition();
            return _tupleTypes.Contains(genericTypeDef);
        }

        // It is Dictionary<string,X> or is concrete and inherits from IDictionary<string,X>
        public static bool IsConcreteDictionaryType(this Type t)
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

        public static bool IsArrayType(this Type t)
            => t.IsArray && t.HasElementType;

        // Is one of IDictionary<string,X> or IReadOnlyDictionary<string,X>
        public static bool IsDictionaryInterfaceType(this Type t)
        {
            if (!t.IsGenericType && !t.IsInterface)
                return false;
            var genericDef = t.GetGenericTypeDefinition();
            return (genericDef == typeof(IDictionary<,>) || genericDef == typeof(IReadOnlyDictionary<,>))
                   && t.GenericTypeArguments[0] == typeof(string);
        }

        public static bool IsConcreteCollectionType(this Type t)
            =>
                !t.IsInterface
                && !t.IsAbstract
                && t.GetInterfaces()
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>));

        public static bool IsAbstractCollectionType(this Type t)
            =>
                t.IsGenericType
                && t.IsInterface
                && (t.Namespace ?? string.Empty).StartsWith("System.Collections");

        private static readonly HashSet<Type> _numericTypes = new HashSet<Type>
        {
            typeof(byte),
            typeof(byte?),
            typeof(decimal),
            typeof(decimal?),
            typeof(double),
            typeof(double?),
            typeof(float),
            typeof(float?),
            typeof(int),
            typeof(int?),
            typeof(long),
            typeof(long?),
            typeof(short),
            typeof(short?),
            typeof(uint),
            typeof(uint?),
            typeof(ulong),
            typeof(ulong?),
            typeof(ushort),
            typeof(ushort?),
        };

        private static readonly HashSet<Type> _primitiveTypes = new HashSet<Type>
        {
            typeof(bool),
            typeof(bool?),
            typeof(byte),
            typeof(byte?),
            typeof(byte[]),
            typeof(DateTime),
            typeof(DateTime?),
            typeof(decimal),
            typeof(decimal?),
            typeof(double),
            typeof(double?),
            typeof(float),
            typeof(float?),
            typeof(Guid),
            typeof(Guid?),
            typeof(int),
            typeof(int?),
            typeof(long),
            typeof(long?),
            typeof(short),
            typeof(short?),
            typeof(string),
            typeof(uint),
            typeof(uint?),
            typeof(ulong),
            typeof(ulong?),
            typeof(ushort),
            typeof(ushort?),
        };

        public static bool IsSupportedPrimitiveType(this Type t) => _primitiveTypes.Contains(t);

        public static bool IsNumericType(this Type t) => _numericTypes.Contains(t);

        public static bool IsMappableScalarType(this Type t)
            => _primitiveTypes.Contains(t) || t == typeof(object);

        public static bool IsUntypedEnumerableType(this Type t)
            => t == typeof(IEnumerable) || t == typeof(IList) || t == typeof(ICollection);

        public static bool IsMappableCustomObjectType(this Type t)
        {
            return t.IsClass
                   && !t.IsInterface
                   // We allow abstract types here, because they might be configured to use a
                   // concrete subtype. We'll check later when we actually try to instantiate
                   // the object
                   //&& !t.IsAbstract
                   && !t.IsArray
                   && t != typeof(string)
                   && !(t.Namespace ?? string.Empty).StartsWith("System.Collections")
                   && t != typeof(object);
        }

        public static object GetDefaultValue(this Type t)
            => t.IsValueType ? Activator.CreateInstance(t) : null;
    }
}
