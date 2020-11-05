using System;
using System.Reflection;

namespace CastIron.Sql.Mapping.Compilers
{
    public struct ConcreteCollectionInfo
    {
        public ConcreteCollectionInfo(Type concreteType, Type elementType, ConstructorInfo constructor, MethodInfo addMethod)
        {
            ConcreteType = concreteType;
            ElementType = elementType;
            AddMethod = addMethod;
            Constructor = constructor;
        }

        public Type ConcreteType { get; }
        public Type ElementType { get; }
        public MethodInfo AddMethod { get; }
        public ConstructorInfo Constructor { get; }

        public void Deconstruct(out Type concreteType, out Type elementType, out ConstructorInfo constructor, out MethodInfo addMethod)
        {
            concreteType = ConcreteType;
            elementType = ElementType;
            constructor = Constructor;
            addMethod = AddMethod;
        }
    }
}