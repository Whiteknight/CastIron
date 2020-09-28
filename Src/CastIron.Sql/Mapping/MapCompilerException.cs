using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace CastIron.Sql.Mapping
{
    /// <summary>
    /// Exception when something goes wrong during map compilation
    /// </summary>
    [Serializable]
    public class MapCompilerException : Exception
    {
        public MapCompilerException()
        {
        }

        public MapCompilerException(string message) : base(message)
        {
        }

        public MapCompilerException(string message, Exception inner) : base(message, inner)
        {
        }

        protected MapCompilerException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }

        public static MapCompilerException NoScalarMapCompilers()
        {
            return new MapCompilerException("No scalar map compilers configured, there's no way to copy data from the result set into an object");
        }

        public static MapCompilerException TypeCannotBeNull()
        {
            return new MapCompilerException("The type specified may not be null.");
        }

        public static MapCompilerException InvalidListType(Type provided)
        {
            return new MapCompilerException(
                $"The provided type {provided.GetFriendlyName()} is not a suitable collection type. " +
                $"It must implement {nameof(IList)}.{nameof(IList<object>.Add)} to be used for mapping"
            );
        }

        public static MapCompilerException InvalidInterfaceType(Type provided)
        {
            return new MapCompilerException(
                $"The provided type {provided.GetFriendlyName()} is an interface type which the map compiler does " +
                $"not support. Consider using one of the interface types {nameof(IEnumerable)}<T>, {nameof(IList)}<T> " +
                $"or {nameof(ICollection)}<T>. Please consult the documentation for the complete list of interface " +
                "types supported by the map compiler.");
        }

        public static MapCompilerException InvalidTupleMap(int paramLength, Type targetType)
        {
            return new MapCompilerException(
                $"Tuple type {targetType.GetFriendlyName()} is invalid mapping target. " +
                $"Tuple types must have between 1-7 type parameters only (you specified {paramLength}). " +
                $"The type must also provide a valid factory method with the given number of parameters in {typeof(Tuple)} class.");
        }

        public static MapCompilerException InvalidDictionaryTargetType(Type targetType)
        {
            return new MapCompilerException(
                $"Dictionary type {targetType.GetFriendlyName()} must inherit from {typeof(IDictionary<,>).GetFriendlyName()}, " +
                $"must contain a default parameterless constructor (mapping of constructor parameters for dictionary types is not supported), " +
                $"and must implement the .{nameof(IDictionary<string, object>.Add)}({typeof(string).Name}, {typeof(object).Name}) method");
        }

        public static MapCompilerException DictionaryTypeMissingAddMethod(Type targetType)
        {
            return new MapCompilerException(
                $"Type {targetType.FullName} looks like a dictionary type but does not provide a " +
                $".{nameof(Dictionary<string, object>.Add)}() Method. The map compiler uses the Add method to " +
                "add data to the dictionary. Without access to this method, the map may not procede.");
        }

        public static MapCompilerException CannotConvertType(Type targetType, Type workingType)
        {
            return new MapCompilerException($"Cannot assign the provided type {targetType.GetFriendlyName()} to working type {workingType.GetFriendlyName()}");
        }

        public static MapCompilerException MissingParameterlessConstructor(Type targetType)
        {
            return new MapCompilerException($"Target type {targetType.GetFriendlyName()} must have a default parameterless constructor");
        }

        public static MapCompilerException MissingMethod(Type targetType, string methodName, params Type[] argumentTypes)
        {
            var argsString = string.Join(",", argumentTypes.Select(a => a.GetFriendlyName()));
            return new MapCompilerException($"Type {targetType.GetFriendlyName()} is missing method .{methodName}({argsString})");
        }

        public static MapCompilerException CannotDetermineArrayElementType(Type targetType)
        {
            return new MapCompilerException($"Cannot find element type for array type {targetType.GetFriendlyName()}");
        }

        public static MapCompilerException MissingArrayConstructor(Type targetType)
        {
            return new MapCompilerException($"Array type {targetType.GetFriendlyName()} must have a standard array constructor");
        }

        public static MapCompilerException TypeUnmappable(Type targetType)
        {
            return new MapCompilerException($"Unable to compile a mapping for {targetType.GetFriendlyName()}. Consider using a simpler type.");
        }

        public static MapCompilerException MapAndCompilerSpecified()
        {
            return new MapCompilerException(
                "Both an explicit map function and a map function compiler have been specified at the same time. " +
                "This is not allowed. You may only specify one or the other. Please set a map function if you have " +
                "one prepared, otherwise specify a map compiler to build one for you.");
        }

        public static MapCompilerException UnconstructableAbstractType(Type targetType)
        {
            return new MapCompilerException(
                $"The type {targetType.GetFriendlyName()} is abstract or an interface, and a suitable constructor cannot be found. " +
                "Make sure you configure objects of this type to map to a concrete type with a public constructor"
            );
        }
    }
}