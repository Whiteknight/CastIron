using System;
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

        public static MapCompilerException InvalidDictionaryTargetType(Type targetType)
        {
            return new MapCompilerException(
                $"Dictionary type {targetType.GetFriendlyName()} must inherit from {typeof(IDictionary<,>).GetFriendlyName()}, " +
                $"must contain a default parameterless constructor (mapping of constructor parameters for dictionary types is not supported), " +
                $"and must implement the .{nameof(IDictionary<string, object>.Add)}({typeof(string).Name}, {typeof(object).Name}) method");
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

        public static MapCompilerException MissingArrayConstructor(Type targetType)
        {
            return new MapCompilerException($"Array type {targetType.GetFriendlyName()} must have a standard array constructor");
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