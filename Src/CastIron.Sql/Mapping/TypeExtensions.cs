using System;
using System.Collections.Generic;
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
