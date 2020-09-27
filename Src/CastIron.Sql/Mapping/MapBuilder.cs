using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using CastIron.Sql.Utility;

namespace CastIron.Sql.Mapping
{
    // MapBuilder creates a TypeSettingsCollection  to hold information about all types in the map
    // During compilation, call MapContext->MapCompileOperation->TypeSettings to get information
    // about constructors, etc. 

    public class TypeSettingsCollection
    {
        private readonly Dictionary<Type, ITypeSettings> _types;
        private readonly ITypeSettings _default;

        public string Separator { get; private set; }
        public ICollection<string> IgnorePrefixes { get; private set; }
        public IMapCompiler DefaultCompiler { get; private set; }

        public TypeSettingsCollection()
        {
            _types = new Dictionary<Type, ITypeSettings>();
            _default = new TypeSettings<object>();
            Separator = "_";
        }

        public void Add(ITypeSettings type)
        {
            _types.Add(type.BaseType, type);
        }

        public ITypeSettings GetTypeSettings(Type type)
        {
            return _types.ContainsKey(type) ? _types[type] : _default;
        }

        public void SetChildSeparator(string separator)
        {
            Separator = separator ?? "_";
        }
    }

    public interface ITypeSettings
    {
        Type BaseType { get; }
        ISpecificTypeSettings GetDefault();
        IEnumerable<ISpecificTypeSettings> GetSpecificTypes();
    }

    public class TypeSettings<T> : ITypeSettings
    {
        private readonly List<ISpecificTypeSettings<T>> _subclasses;

        public TypeSettings()
        {
            Default = new SpecificTypeSettings<T, T>
            {
                Predicate = r => true,
                Type = typeof(T)
            };
            BaseType = typeof(T);
            _subclasses = new List<ISpecificTypeSettings<T>>();
        }

        public void AddType(ISpecificTypeSettings<T> specific)
        {
            _subclasses.Add(specific);
        }

        public ISpecificTypeSettings GetDefault() => Default;
        public ISpecificTypeSettings<T> Default { get; private set; }

        public Type BaseType { get; }

        public IEnumerable<ISpecificTypeSettings> GetSpecificTypes() => _subclasses ?? Enumerable.Empty<ISpecificTypeSettings>();
    }

    public interface ISpecificTypeSettings
    {
        Func<IDataRecord, bool> Predicate { get; }
        ConstructorInfo Constructor { get; }
        void SetConstructor(ConstructorInfo constructor);
        void SetConstructorFinder(IConstructorFinder finder);
        void SetType(Type t);
        SubclassMapper CreateMapper(IProviderConfiguration provider, IMapCompiler compiler, TypeSettingsCollection typeSettings, IDataReader reader);
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

        public SubclassMapper CreateMapper(IProviderConfiguration provider, IMapCompiler compiler, TypeSettingsCollection typeSettings, IDataReader reader)
        {
            // First, check a few invariants
            Argument.NotNull(compiler, nameof(compiler));
            if (Type == null)
                throw new InvalidOperationException("The type specified may not be null");
            if (!typeof(T).IsAssignableFrom(Type))
                throw new InvalidOperationException($"Type {Type.Name} must be a concrete class type which is assignable to {typeof(T).Name}.");

            // If we already have a map, we're done. Otherwise let's compile one with our given
            // compiler options
            if (Mapper != null)
                return new SubclassMapper(Predicate, r => Mapper(r));

            var context = new MapCompileOperation(provider, reader, Type, typeSettings);
            Mapper = compiler.CompileExpression<T>(context, reader);

            return new SubclassMapper(Predicate, r => Mapper(r));
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

    // The final, compiled mapper for the type, with all other information discarded
    public class SubclassMapper
    {
        public SubclassMapper(Func<IDataRecord, bool> predicate, Func<IDataRecord, object> mapper)
        {
            Predicate = predicate;
            Mapper = mapper;
        }

        public Func<IDataRecord, bool> Predicate { get; }
        public Func<IDataRecord, object> Mapper { get; }
    }

    public class MapBuilder : IMapCompilerSettings
    {
        private readonly TypeSettingsCollection _types;

        public MapBuilder(TypeSettingsCollection types)
        {
            _types = types;
        }

        public IMapCompilerSettings For<T>(Action<IMapCompilerSettings<T>> setup)
        {
            var builder = new MapBuilder<T>(_types);
            setup?.Invoke(builder);
            return this;
        }

        public IMapCompilerSettings UseChildSeparator(string separator)
        {
            _types.SetChildSeparator(separator);
            return this;
        }
    }

    /// <summary>
    /// A facade to build up TypeSettingsCollection using IMapCompilerSettings<T> interface
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MapBuilder<T> : IMapCompilerSettings<T>
    {
        private readonly ISpecificTypeSettings<T> _specific;
        private readonly TypeSettingsCollection _types;
        private readonly TypeSettings<T> _currentType;

        public MapBuilder(TypeSettingsCollection types)
        {
            _types = types;
            _currentType = new TypeSettings<T>();
            _types.Add(_currentType);
            _specific = _currentType.Default;
        }

        private MapBuilder(TypeSettingsCollection types, TypeSettings<T> type, ISpecificTypeSettings<T> specific)
        {
            _types = types;
            _currentType = type;
            _specific = specific;
        }

        public IMapCompilerSettingsBase<T> UseMap(Func<IDataRecord, T> map)
        {
            Argument.NotNull(map, nameof(map));
            _specific.SetMap(map);
            return this;
        }

        public IMapCompilerSettingsBase<T> UseConstructor(ConstructorInfo constructor)
        {
            Argument.NotNull(constructor, nameof(constructor));
            _specific.SetConstructor(constructor);
            return this;
        }

        public IMapCompilerSettingsBase<T> UseConstructor(params Type[] argumentTypes)
        {
            var constructor = typeof(T).GetConstructor(argumentTypes) ?? throw ConstructorFindException.NoSuitableConstructorFound(typeof(T));
            return UseConstructor(constructor);
        }

        public IMapCompilerSettings<T> UseConstructorFinder(IConstructorFinder finder)
        {
            Argument.NotNull(finder, nameof(finder));
            _specific.SetConstructorFinder(finder);
            return this;
        }

        public IMapCompilerSettingsBase<T> UseFactoryMethod(Func<T> factory)
        {
            Argument.NotNull(factory, nameof(factory));
            _specific.SetFactoryMethod(factory);
            return this;
        }

        public IMapCompilerSettings<T> UseClass<TSpecific>()
            where TSpecific : T
        {
            _specific.SetType(typeof(TSpecific));
            return this;
        }

        public IMapCompilerSettings<T> UseSubclass<TSubclass>(Func<IDataRecord, bool> predicate, Action<IMapCompilerSettingsBase<T>> setup = null)
            where TSubclass : T
        {
            Argument.NotNull(predicate, nameof(predicate));

            var predicateContext = new SpecificTypeSettings<T, TSubclass>();
            predicateContext.SetType(typeof(TSubclass));
            predicateContext.Predicate = predicate;

            if (setup != null)
            {
                var specific = new SpecificTypeSettings<T, TSubclass>();
                _currentType.AddType(specific);
                var subclassContext = new MapBuilder<T>(_types, _currentType, specific);
                setup.Invoke(subclassContext);
            }
            _currentType.AddType(predicateContext);
            return this;
        }

        // Uses all the configured options to invoke the compiler and build a map function. 
        //public Func<IDataRecord, T> Build(IDataReader reader, IMapCompiler compiler)
        //{
        //    Argument.NotNull(reader, nameof(reader));
        //    Argument.NotNull(compiler, nameof(compiler));

        //    // 1. Compile a mapper for every possible subclass and creation options combo
        //    // The cache will prevent recompilation of same maps so we won't worry about .Distinct() here.
        //    var mappers = new List<SubclassMapper>();
        //    foreach (var subclass in _subclasses)
        //    {
        //        var mapper = subclass.CreateMapper(_provider, compiler, _types, reader);
        //        mappers.Add(mapper);
        //    }

        //    var defaultCaseMapper = _specific.CreateMapper(_provider, compiler, _types, reader);
        //    mappers.Add(defaultCaseMapper);

        //    // 2. Create a thunk which checks each predicate and calls the correct mapper
        //    return CreateThunkExpression(mappers);
        //}

        //private static Func<IDataRecord, T> CreateThunkExpression(IReadOnlyList<SubclassMapper> subclasses)
        //{
        //    if (subclasses.Count == 0)
        //        return r => default;
        //    if (subclasses.Count == 1)
        //        return subclasses[0].Mapper;

        //    return r =>
        //    {
        //        foreach (var subclass in subclasses)
        //        {
        //            if (subclass.Mapper == null || !subclass.Predicate(r))
        //                continue;
        //            return subclass.Mapper(r);
        //        }

        //        return default;
        //    };
        //}
    }
}