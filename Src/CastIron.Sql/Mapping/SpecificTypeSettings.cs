using System;
using System.Data;
using System.Reflection;

namespace CastIron.Sql.Mapping
{
    /// <summary>
    /// Holds configuration for a specific type or subtype, which may be instantiated in place of
    /// a parent type
    /// </summary>
    public interface ISpecificTypeSettings
    {
        Func<IDataRecord, bool> Predicate { get; }
        ConstructorInfo Constructor { get; }
        void SetConstructor(ConstructorInfo constructor);
        void SetConstructorFinder(IConstructorFinder finder);
        void SetType(Type t);
        Func<object> GetFactory();
        IConstructorFinder ConstructorFinder { get; }
        Type Type { get; }
    }

    public interface ISpecificTypeSettings<T> : ISpecificTypeSettings
    {
        public Func<T> Factory { get; }
        void SetFactoryMethod(Func<T> factory);
        void SetMap(Func<IDataRecord, T> map);
    }

    // Represents a single type to construct, and holds all mapping information for that type
    public class SpecificTypeSettings<T, TSpecific> : ISpecificTypeSettings<T>
        where TSpecific : T
    {
        public Type Type { get; set; }
        public Func<IDataRecord, bool> Predicate { get; set; }
        public IConstructorFinder ConstructorFinder { get; private set; }
        public Func<T> Factory { get; private set; }
        public ConstructorInfo Constructor { get; private set; }
        public Func<IDataRecord, T> Mapper { get; set; }

        private static void ThrowMappingAlreadyConfiguredException()
        {
            throw new InvalidOperationException(
                $"A Mapping has already been configured and a second mapping may not be configured for the same type {typeof(T).Name}. " +
                $"Method {nameof(SetMap)} may not be called in conjunction with any of {nameof(SetConstructor)}, {nameof(SetConstructorFinder)} and {nameof(SetFactoryMethod)}). " +
                $"Methods {nameof(SetConstructor)}, {nameof(SetConstructorFinder)} and {nameof(SetFactoryMethod)} are exclusive and at most one of these may be called once."
            );
        }

        public Func<object> GetFactory() => Factory == null ? (Func<object>)null : () => Factory();

        public void SetMap(Func<IDataRecord, T> map)
        {
            if (Mapper != null || Factory != null || Constructor != null)
                ThrowMappingAlreadyConfiguredException();
            Mapper = map;
        }

        public void SetConstructor(ConstructorInfo constructor)
        {
            if (Mapper != null || Constructor != null || Factory != null)
                ThrowMappingAlreadyConfiguredException();
            var type = Type ?? typeof(T);
            if (!type.IsAssignableFrom(constructor.DeclaringType))
            {
                throw new InvalidOperationException(
                    "The specified constructor does not create the correct type of object. " +
                    $"Expecting {type.GetFriendlyName()} but constructor builds {constructor.DeclaringType.GetFriendlyName()}");
            }
            Constructor = constructor;
        }

        public void SetConstructorFinder(IConstructorFinder finder)
        {
            if (Mapper != null)
                throw MapCompilerException.MapAndCompilerSpecified();
            if (ConstructorFinder != null)
                throw new InvalidOperationException("Attempt to specify two constructor finders. You may only have one constructor finder per map attempt.");
            ConstructorFinder = finder;
        }

        public void SetFactoryMethod(Func<T> factory)
        {
            if (Mapper != null || Constructor != null || Factory != null)
                ThrowMappingAlreadyConfiguredException();
            Factory = factory;
        }

        public void SetType(Type t)
        {
            AssertValidType(t);
            if (Type != null && Type != typeof(T))
                throw new InvalidOperationException("Cannot specify more than one specific class.");
            if (Factory != null)
                throw new InvalidOperationException("May not specify a specific subclass and a factory method at the same time.");
            if (Constructor != null)
                throw new InvalidOperationException("May not specify a particular constructor before specifying the subclass.");
            Type = t;
        }

        private static void AssertValidType(Type t)
        {
            if (t == null)
                throw new ArgumentNullException(nameof(t), $"The specified output type used for this compiler may not be null. It must be a valid, concrete class assignable to {typeof(T).Name}");
            if (t.IsAbstract || t.IsInterface || !typeof(T).IsAssignableFrom(t))
                throw new InvalidOperationException($"Type {t.Name} must be a concrete class type which is assignable to {typeof(T).Name}.");
        }
    }
}