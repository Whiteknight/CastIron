﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using CastIron.Sql.Utility;

namespace CastIron.Sql.Mapping
{
    public class MapCompilerBuilder<T> : IMapCompilerBuilder<T>
    {
        private readonly IProviderConfiguration _provider;
        private readonly List<SubclassPredicate> _subclasses;
        private readonly SubclassPredicate _defaultCase;

        public MapCompilerBuilder(IProviderConfiguration provider)
        {
            _provider = provider;
            _subclasses = new List<SubclassPredicate>();
            _defaultCase = new SubclassPredicate
            {
                Predicate = r => true,
                Type = typeof(T)
            };
        }

        private MapCompilerBuilder(SubclassPredicate defaultCase)
        {
            _defaultCase = defaultCase;
        }

        public IMapCompilerBuilderBase<T> UseMap(Func<IDataRecord, T> map)
        {
            Argument.NotNull(map, nameof(map));
            _defaultCase.SetMap(map);
            return this;
        }

        public IMapCompilerBuilderBase<T> UseCompiler(IMapCompiler compiler)
        {
            Argument.NotNull(compiler, nameof(compiler));
            _defaultCase.SetCompiler(compiler);
            return this;
        }

        public IMapCompilerBuilderBase<T> UseConstructor(ConstructorInfo constructor)
        {
            Argument.NotNull(constructor, nameof(constructor));
            _defaultCase.SetConstructor(constructor);
            return this;
        }

        public IMapCompilerBuilderBase<T> UseConstructor(params Type[] argumentTypes)
        {
            var constructor = typeof(T).GetConstructor(argumentTypes);
            if (constructor == null)
                throw ConstructorFindException.NoSuitableConstructorFound(typeof(T));
            return UseConstructor(constructor);
        }

        public IMapCompilerBuilderBase<T> UseConstructorFinder(IConstructorFinder finder)
        {
            Argument.NotNull(finder, nameof(finder));
            _defaultCase.SetConstructorFinder(finder);
            return this;
        }

        public IMapCompilerBuilderBase<T> UseFactoryMethod(Func<T> factory)
        {
            Argument.NotNull(factory, nameof(factory));
            _defaultCase.SetFactoryMethod(factory);
            return this;
        }

        public IMapCompilerBuilderBase<T> UseChildSeparator(string separator)
        {
            _defaultCase.SetSeparator(separator);
            return this;
        }

        public IMapCompilerBuilder<T> UseClass<TSpecific>() 
            where TSpecific : T
        {
            _defaultCase.SetType(typeof(TSpecific));
            return this;
        }

        public Func<IDataRecord, T> Compile(IDataReader reader)
        {
            var defaultCompiler = _defaultCase.Compiler ?? MapCompilation.GetDefaultInstance();

            // 1. Compile a mapper for every possible subclass and creation options combo
            // The cache will prevent recompilation of same maps so we won't worry about .Distinct() here.
            var newSubclasses = new List<SubclassPredicate>();
            foreach (var subclass in _subclasses)
                VerifyAndSetupSubclassPredicate(reader, defaultCompiler, subclass, newSubclasses);
            VerifyAndSetupSubclassPredicate(reader, defaultCompiler, _defaultCase, newSubclasses);

            // 2. Create a thunk which checks each predicate and calls the correct mapper
            return CreateThunkExpression(newSubclasses);
        }

        public IMapCompilerBuilder<T> UseSubclass<TSubclass>(Func<IDataRecord, bool> predicate, Action<IMapCompilerBuilderBase<T>> setup = null) 
            where TSubclass : T
        {
            Argument.NotNull(predicate, nameof(predicate));

            var predicateContext = new SubclassPredicate();
            predicateContext.SetType(typeof(TSubclass));
            predicateContext.Predicate = predicate;

            var subclassContext = new MapCompilerBuilder<T>(predicateContext);
            
            setup?.Invoke(subclassContext);
            _subclasses.Add(predicateContext);
            return this;
        }

        private static Func<IDataRecord, T> CreateThunkExpression(IReadOnlyList<SubclassPredicate> subclasses)
        {
            if (subclasses.Count == 0)
                return r => default;
            if (subclasses.Count == 1)
                return subclasses[0].Mapper;

            return r =>
            {
                foreach (var subclass in subclasses)
                {
                    if (!subclass.Predicate(r))
                        continue;
                    if (subclass.Mapper == null)
                        continue;
                    var result = subclass.Mapper(r);

                    return result;
                }

                return default;
            };
        }

        private void VerifyAndSetupSubclassPredicate(IDataReader reader, IMapCompiler defaultCompiler, SubclassPredicate subclass, List<SubclassPredicate> newSubclasses)
        {
            if (subclass.Type == null)
                throw new InvalidOperationException("The type specified may not be null");
            if (!typeof(T).IsAssignableFrom(subclass.Type))
                throw new InvalidOperationException($"Type {subclass.Type.Name} must be a concrete class type which is assignable to {typeof(T).Name}.");
            if (subclass.Mapper == null)
            {
                var context = new MapCompileContext(_provider, subclass.Type, subclass.Factory as Func<object>, subclass.Constructor, subclass.ConstructorFinder, subclass.Separator);
                context.PopulateColumnLookups(reader);
                subclass.Mapper = (subclass.Compiler ?? defaultCompiler).CompileExpression<T>(context, reader);
            }

            newSubclasses.Add(subclass);
        }

        private static void AssertValidType(Type t)
        {
            if (t == null)
                throw new ArgumentNullException(nameof(t), $"The specified output type used for this compiler may not be null. It must be a valid, concrete class assignable to {typeof(T).Name}");
            if (t.IsAbstract || t.IsInterface || !typeof(T).IsAssignableFrom(t))
                throw new InvalidOperationException($"Type {t.Name} must be a concrete class type which is assignable to {typeof(T).Name}.");
        }

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

            private static void ThrowMappingAlreadyConfiguredException()
            {
                throw new InvalidOperationException(
                    $"A Mapping has already been configured and a second mapping may not be configured for the same type {typeof(T).Name}. " +
                    $"Method {nameof(SetMap)} may not be called in conjunction with any of {nameof(SetCompiler)}, {nameof(SetConstructor)}, {nameof(SetConstructorFinder)} and {nameof(SetFactoryMethod)}). " +
                    $"Methods {nameof(SetConstructor)}, {nameof(SetConstructorFinder)} and {nameof(SetFactoryMethod)} are exclusive and at most one of these may be called once."
                );
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
                // TODO: We can probably clean up or ease some of these restrictions
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
        }
    }
}