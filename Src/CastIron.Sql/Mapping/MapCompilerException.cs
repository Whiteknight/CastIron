using System;
using System.Collections.Generic;
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

        public static MapCompilerException InvalidCollectionType(Type targetType)
        {
            return new MapCompilerException(
                $"Collection type {targetType.GetFriendlyName()} must inherit from IEnumerable or IEnumerable<T>, " +
                "must have a default parameterless constructor, and must have a public .Add(T) method.");
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