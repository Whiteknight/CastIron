using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace CastIron.Sql.Mapping
{
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
            return new MapCompilerException($"Cannot find element type for arraytype {targetType.GetFriendlyName()}");
        }

        public static MapCompilerException MissingArrayConstructor(Type targetType)
        {
            return new MapCompilerException($"Array type {targetType.GetFriendlyName()} must have a standard array constructor");
        }

        public static MapCompilerException TypeUnmappable(Type targetType)
        {
            return new MapCompilerException($"Unable to compile a mapping for {targetType.GetFriendlyName()}. Consider using a simpler type.");
        }
    }
}