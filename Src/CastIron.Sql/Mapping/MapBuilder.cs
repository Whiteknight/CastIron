using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using CastIron.Sql.Utility;

namespace CastIron.Sql.Mapping
{
    /// <summary>
    /// Builder class for mapping functions. The builder takes several options for settings and
    /// preferences, and then invokes the specified compilers to create a mapping function for the
    /// given type T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MapBuilder<T> : IMapCompilerSettings<T>
    {
        private readonly IProviderConfiguration _provider;
        private readonly List<SubclassPredicate> _subclasses;
        private readonly SubclassPredicate _defaultCase;

        public MapBuilder(IProviderConfiguration provider)
        {
            _provider = provider;
            _subclasses = new List<SubclassPredicate>();
            _defaultCase = new SubclassPredicate
            {
                Predicate = r => true,
                Type = typeof(T)
            };
        }

        private MapBuilder(SubclassPredicate defaultCase)
        {
            _defaultCase = defaultCase;
        }

        public IMapCompilerSettingsBase<T> UseMap(Func<IDataRecord, T> map)
        {
            Argument.NotNull(map, nameof(map));
            _defaultCase.SetMap(map);
            return this;
        }

        public IMapCompilerSettingsBase<T> UseCompiler(IMapCompiler compiler)
        {
            Argument.NotNull(compiler, nameof(compiler));
            _defaultCase.SetCompiler(compiler);
            return this;
        }

        public IMapCompilerSettingsBase<T> UseConstructor(ConstructorInfo constructor)
        {
            Argument.NotNull(constructor, nameof(constructor));
            _defaultCase.SetConstructor(constructor);
            return this;
        }

        public IMapCompilerSettingsBase<T> UseConstructor(params Type[] argumentTypes)
        {
            var constructor = typeof(T).GetConstructor(argumentTypes) ?? throw ConstructorFindException.NoSuitableConstructorFound(typeof(T));
            return UseConstructor(constructor);
        }

        public IMapCompilerSettingsBase<T> UseConstructorFinder(IConstructorFinder finder)
        {
            Argument.NotNull(finder, nameof(finder));
            _defaultCase.SetConstructorFinder(finder);
            return this;
        }

        public IMapCompilerSettingsBase<T> UseFactoryMethod(Func<T> factory)
        {
            Argument.NotNull(factory, nameof(factory));
            _defaultCase.SetFactoryMethod(factory);
            return this;
        }

        public IMapCompilerSettingsBase<T> UseChildSeparator(string separator)
        {
            _defaultCase.SetSeparator(separator);
            return this;
        }

        public IMapCompilerSettings<T> UseClass<TSpecific>()
            where TSpecific : T
        {
            _defaultCase.SetType(typeof(TSpecific));
            return this;
        }

        public IMapCompilerSettings<T> UseSubclass<TSubclass>(Func<IDataRecord, bool> predicate, Action<IMapCompilerSettingsBase<T>> setup = null)
            where TSubclass : T
        {
            Argument.NotNull(predicate, nameof(predicate));

            var predicateContext = new SubclassPredicate();
            predicateContext.SetType(typeof(TSubclass));
            predicateContext.Predicate = predicate;

            if (setup != null)
            {
                var subclassContext = new MapBuilder<T>(predicateContext);
                setup.Invoke(subclassContext);
            }
            _subclasses.Add(predicateContext);
            return this;
        }

        public IMapCompilerSettings<T> IgnorePrefixes(params string[] prefixes)
        {
            _defaultCase.SetIgnorePrefixes(prefixes);
            return this;
        }

        // Uses all the configured options to invoke the compiler and build a map function. 
        public Func<IDataRecord, T> Build(IDataReader reader, IMapCompiler defaultCompiler)
        {
            Argument.NotNull(reader, nameof(reader));
            Argument.NotNull(defaultCompiler, nameof(defaultCompiler));

            // 1. Compile a mapper for every possible subclass and creation options combo
            // The cache will prevent recompilation of same maps so we won't worry about .Distinct() here.
            var mappers = new List<SubclassMapper>();
            foreach (var subclass in _subclasses)
            {
                var thisDefaultCompiler = _defaultCase.Compiler ?? defaultCompiler;
                var mapper = subclass.CreateMapper(_provider, _defaultCase.IgnorePrefixes, thisDefaultCompiler, reader);
                mappers.Add(mapper);
            }

            var defaultCaseMapper = _defaultCase.CreateMapper(_provider, _defaultCase.IgnorePrefixes, defaultCompiler, reader);
            mappers.Add(defaultCaseMapper);

            // 2. Create a thunk which checks each predicate and calls the correct mapper
            return CreateThunkExpression(mappers);
        }

        private static Func<IDataRecord, T> CreateThunkExpression(IReadOnlyList<SubclassMapper> subclasses)
        {
            if (subclasses.Count == 0)
                return r => default;
            if (subclasses.Count == 1)
                return subclasses[0].Mapper;

            return r =>
            {
                foreach (var subclass in subclasses)
                {
                    if (subclass.Mapper == null || !subclass.Predicate(r))
                        continue;
                    return subclass.Mapper(r);
                }

                return default;
            };
        }

        private static void AssertValidType(Type t)
        {
            if (t == null)
                throw new ArgumentNullException(nameof(t), $"The specified output type used for this compiler may not be null. It must be a valid, concrete class assignable to {typeof(T).Name}");
            if (t.IsAbstract || t.IsInterface || !typeof(T).IsAssignableFrom(t))
                throw new InvalidOperationException($"Type {t.Name} must be a concrete class type which is assignable to {typeof(T).Name}.");
        }

        // The final, compiled mapper for the type, with all other information discarded
        private class SubclassMapper
        {
            public SubclassMapper(Func<IDataRecord, bool> predicate, Func<IDataRecord, T> mapper)
            {
                Predicate = predicate;
                Mapper = mapper;
            }

            public Func<IDataRecord, bool> Predicate { get; }
            public Func<IDataRecord, T> Mapper { get; }
        }

        // Represents a single type to construct, and holds all mapping information for that type
        private class SubclassPredicate
        {
            public Type Type { get; set; }
            public Func<IDataRecord, bool> Predicate { get; set; }
            public IMapCompiler Compiler { get; private set; }
            public IConstructorFinder ConstructorFinder { get; private set; }
            public Func<T> Factory { get; private set; }
            public ConstructorInfo Constructor { get; private set; }
            public Func<IDataRecord, T> Mapper { get; set; }
            public string Separator { get; private set; }
            public ICollection<string> IgnorePrefixes { get; private set; }

            private static void ThrowMappingAlreadyConfiguredException()
            {
                throw new InvalidOperationException(
                    $"A Mapping has already been configured and a second mapping may not be configured for the same type {typeof(T).Name}. " +
                    $"Method {nameof(SetMap)} may not be called in conjunction with any of {nameof(SetCompiler)}, {nameof(SetConstructor)}, {nameof(SetConstructorFinder)} and {nameof(SetFactoryMethod)}). " +
                    $"Methods {nameof(SetConstructor)}, {nameof(SetConstructorFinder)} and {nameof(SetFactoryMethod)} are exclusive and at most one of these may be called once."
                );
            }

            public SubclassMapper CreateMapper(IProviderConfiguration provider, ICollection<string> ignorePrefixes, IMapCompiler defaultCompiler, IDataReader reader)
            {
                // First, check a few invariants
                if (Type == null)
                    throw new InvalidOperationException("The type specified may not be null");
                if (!typeof(T).IsAssignableFrom(Type))
                    throw new InvalidOperationException($"Type {Type.Name} must be a concrete class type which is assignable to {typeof(T).Name}.");

                // If we already have a map, we're done. Otherwise let's compile one with our given
                // compiler options
                if (Mapper != null)
                    return new SubclassMapper(Predicate, Mapper);

                var createPrefs = new ObjectCreatePreferences(ConstructorFinder);
                createPrefs.AddType(Type, Factory as Func<object>, Constructor);
                var context = new MapCompileOperation(provider, Type, createPrefs, Separator, ignorePrefixes);
                Mapper = (Compiler ?? defaultCompiler).CompileExpression<T>(context, reader);

                return new SubclassMapper(Predicate, Mapper);
            }

            public void SetMap(Func<IDataRecord, T> map)
            {
                if (Mapper != null || Compiler != null || Factory != null || Constructor != null)
                    ThrowMappingAlreadyConfiguredException();
                Mapper = map;
            }

            public void SetCompiler(IMapCompiler compiler)
            {
                if (Mapper != null || Compiler != null)
                    ThrowMappingAlreadyConfiguredException();
                Compiler = compiler;
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

            public void SetSeparator(string separator)
            {
                Separator = separator;
            }

            public void SetIgnorePrefixes(ICollection<string> prefixes)
            {
                IgnorePrefixes = prefixes;
            }
        }
    }
}