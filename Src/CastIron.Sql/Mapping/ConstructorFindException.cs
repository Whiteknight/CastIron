using System;
using System.Runtime.Serialization;

namespace CastIron.Sql.Mapping
{
    [Serializable]
    public class ConstructorFindException : Exception
    {
        public ConstructorFindException()
        {
        }

        public ConstructorFindException(string message) : base(message)
        {
        }

        public ConstructorFindException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ConstructorFindException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }

        public static ConstructorFindException NonInvokableConstructorSpecified()
        {
            return new ConstructorFindException(
                "The specified constructor is static or non-public and cannot be used to construct an object. " +
                "Please provide a suitable constructor or, if you are not sure, allow the system to find one.");
        }

        public static ConstructorFindException InvalidDeclaringType(Type declaringType, Type expected)
        {
            if (declaringType == null)
            {
                return new ConstructorFindException(
                    $"The constructor specified does not have a declaring type and is not valid. " +
                    $"Expected a constructor of type {expected.GetFriendlyName()}.");
            }

            return new ConstructorFindException(
                $"Specified constructor is for type {declaringType.GetFriendlyName()} instead of the expected {expected.GetFriendlyName()}. " +
                "If you specify a custom constructor, it must construct objects of the specified type.");
        }

        public static ConstructorFindException NoDefaultConstructor(Type type)
        {
            return new ConstructorFindException(
                $"The type {type.GetFriendlyName()} does not contain a default parameterless constructor. " +
                "The mapper has been instructed to only use a default parameterless constructor and so it cannot procede. " +
                "You can either add the missing constructor to your code or you can allow the mapper to search for the " +
                "best constructor or you can manually specify an appropriate constructor to use.");
        }

        public static ConstructorFindException NoSuitableConstructorFound(Type type)
        {
            return new ConstructorFindException(
                $"Cannot find a suitable constructor for type {type.GetFriendlyName()}. " + 
                "Usable constructors must be public, non-static and all arguments must be of mappable types." +
                "Consult the documentation for a list of mappable types.");
        }
    }
}