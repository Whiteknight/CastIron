using System;
using System.Runtime.Serialization;

namespace CastIron.Sql.Mapping
{
    /// <summary>
    /// Exception when something goes wrong during mapping
    /// </summary>
    [Serializable]
    public class DataMappingException : Exception
    {
        public DataMappingException()
        {
        }

        public DataMappingException(string message) : base(message)
        {
        }

        public DataMappingException(string message, Exception inner) : base(message, inner)
        {
        }

        protected DataMappingException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }

        public static DataMappingException UserFactoryReturnedNull(Type type)
        {
            return new DataMappingException(
                "The provided factory method has returned a null value, or a value which cannot " +
                $"be converted to type  {type.GetFriendlyName()}. The compiled map routine cannot " +
                "continue. Please ensure that the provided factory method returns a non-null value " +
                "of a compatible type, or use a different mechanism for creating objects (constructor, etc)");
        }
    }
}