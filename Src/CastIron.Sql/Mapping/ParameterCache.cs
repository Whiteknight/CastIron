using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;

namespace CastIron.Sql.Mapping
{
    public class ParameterCache
    {
        private readonly IReadOnlyDictionary<string, object> _parameterCache;

        public ParameterCache(IDbCommand command)
        {
            _parameterCache = command?.Parameters
                ?.Cast<DbParameter>()
                ?.ToDictionary(p => CanonicalizeParameterName(p.ParameterName), p => p.Value) 
                ?? new Dictionary<string, object>();
        }

        private static string CanonicalizeParameterName(string name) 
            => (name.StartsWith("@") ? name.Substring(1) : name).ToLowerInvariant();

        public object GetValue(string name)
        {
            var parameterName = CanonicalizeParameterName(name);
            if (!_parameterCache.ContainsKey(parameterName))
                return null;
            var value = _parameterCache[parameterName];
            return value == null || value == DBNull.Value ? null : value;
        }

        public static Exception ValueNotFound(string name) 
            => new DataReaderException($"Could not find parameter named {name}. Please check your query and your spelling and try again.");

        public object GetValueOrThrow(string name)
        {
            var parameterName = CanonicalizeParameterName(name);
            if (!_parameterCache.ContainsKey(parameterName))
                throw ValueNotFound(name);
            var value = _parameterCache[parameterName];
            return value == null || value == DBNull.Value ? null : value;
        }

        public T GetValue<T>(string name)
        {
            var value = GetValue(name);
            if (value == null)
                return default;
            if (typeof(T) == typeof(object))
                return (T)value;
            if (value is T asT)
                return asT;
            if (value is IConvertible && typeof(IConvertible).IsAssignableFrom(typeof(T)))
                return (T)Convert.ChangeType(value, typeof(T));

            return default;
        }

        public T GetOutputParameterOrThrow<T>(string name)
        {
            var value = GetValueOrThrow(name);
            if (value == null)
                return default;
            if (typeof(T) == typeof(object))
                return (T)value;
            if (value is T asT)
                return asT;
            if (typeof(T) == typeof(string))
                return (T)(object)value.ToString();
            if (value is IConvertible && typeof(IConvertible).IsAssignableFrom(typeof(T)))
                return (T)Convert.ChangeType(value, typeof(T));

            throw new DataReaderException($"Cannot get output parameter value '{name}'. Expected type {typeof(T).GetFriendlyName()} but found {value.GetType().GetFriendlyName()} and no conversion can be found.");
        }

        public T GetOutputParameters<T>()
            where T : class, new()
        {
            // TODO: Can we introspect and get constructor parameters by name?
            var t = new T();
            var properties = typeof(T)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .ToList();
            foreach (var property in properties)
                Assign(t, property, CanonicalizeParameterName(property.Name));

            return t;
        }

        private void Assign(object t, PropertyInfo property, string name)
        {
            var value = GetValue(name);
            var propertyType = property.PropertyType;
            if (propertyType == value.GetType())
                property.SetValue(t, value);
            else if (propertyType == typeof(object))
                property.SetValue(t, value);
            else if (propertyType == typeof(string))
                property.SetValue(t, value.ToString());
            else if (typeof(IConvertible).IsAssignableFrom(propertyType) && value is IConvertible)
                property.SetValue(t, Convert.ChangeType(value, propertyType));
        }
    }
}
