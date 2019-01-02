using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using CastIron.Sql.Utility;

namespace CastIron.Sql.Mapping
{
    public class MapCompilerBuilder<T> : IMapCompilerBuilder<T>
    {
        private readonly List<SubclassPredicate> _subclasses;
        private readonly SubclassPredicate _defaultCase;

        private class SubclassPredicate
        {
            public Type Type { get; set; }
            public Func<IDataRecord, bool> Predicate { get; set; }
            public IMapCompiler Compiler { get; private set; }
            public IConstructorFinder ConstructorFinder { get; private set; }
            public Func<T> Factory { get; private set; }
            public ConstructorInfo Constructor { get; private set; }
            public Func<IDataRecord, T> Mapper { get; set; }

            public void SetMap(Func<IDataRecord, T> map)
            {
                if (Mapper != null)
                    throw new Exception("May not specify two mappings");
                if (Compiler != null || Factory != null || Constructor != null)
                    throw new Exception("May not specify a explicit mapping and compiler details at the same time");
                Mapper = map;
            }

            public void SetCompiler(IMapCompiler compiler)
            {
                if (Mapper != null)
                    throw new Exception("May not specify a explicit mapping and compiler details at the same time");
                if (Compiler != null)
                    throw new Exception("May not specify two compilers for a single mapping");
                Compiler = compiler;
            }

            public void SetConstructor(ConstructorInfo constructor)
            {
                if (Mapper != null)
                    throw new Exception("May not specify a explicit mapping and compiler details at the same time");
                if (Constructor != null)
                    throw new Exception("Cannot specify a second constructor");
                if (Factory != null)
                    throw new Exception("May not specify a constructor if a factory method is being used.");
                var type = Type ?? typeof(T);
                if (!type.IsAssignableFrom(constructor.DeclaringType))
                    throw new Exception("The specified constructor does not create the correct type of object");
                Constructor = constructor;
            }

            public void SetConstructorFinder(IConstructorFinder finder)
            {
                if (Mapper != null)
                    throw new Exception("May not specify a explicit mapping and compiler details at the same time");
                if (ConstructorFinder != null)
                    throw new Exception("May not specify two constructor finders");
                ConstructorFinder = finder;
            }

            public void SetFactoryMethod(Func<T> factory)
            {
                if (Mapper != null)
                    throw new Exception("May not specify a explicit mapping and compiler details at the same time");
                if (Constructor != null)
                    throw new Exception("May not specify a factory method if a constructor is being used");
                if (Factory != null)
                    throw new Exception("May not specify two factory methods");
                Factory = factory;
            }

            public void SetType(Type t)
            {
                if (t.IsAbstract || t.IsInterface)
                    throw new Exception("Type must be concrete");
                if (Type != null && Type != typeof(T))
                    throw new Exception("Cannot specify more than one specific class");
                if (Factory != null)
                    throw new Exception("May not specify a specific subclass and a factory method at the same time");
                if (Constructor != null)
                    throw new Exception("May not specify a particular constructor before specifying the subclass");
                Type = t;
            }
        }

        public MapCompilerBuilder()
        {
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
            Assert.ArgumentNotNull(map, nameof(map));
            _defaultCase.SetMap(map);
            return this;
        }

        public IMapCompilerBuilderBase<T> UseCompiler(IMapCompiler compiler)
        {
            Assert.ArgumentNotNull(compiler, nameof(compiler));
            _defaultCase.SetCompiler(compiler);
            return this;
        }

        public IMapCompilerBuilderBase<T> UseConstructor(ConstructorInfo constructor)
        {
            Assert.ArgumentNotNull(constructor, nameof(constructor));
            _defaultCase.SetConstructor(constructor);
            return this;
        }

        public IMapCompilerBuilderBase<T> UseConstructor(params Type[] argumentTypes)
        {
            var constructor = typeof(T).GetConstructor(argumentTypes);
            if (constructor == null)
                throw new Exception("Cannot find constructor with given argument list");
            return UseConstructor(constructor);
        }

        public IMapCompilerBuilderBase<T> UseConstructorFinder(IConstructorFinder finder)
        {
            Assert.ArgumentNotNull(finder, nameof(finder));
            _defaultCase.SetConstructorFinder(finder);
            return this;
        }

        public IMapCompilerBuilderBase<T> UseFactoryMethod(Func<T> factory)
        {
            Assert.ArgumentNotNull(factory, nameof(factory));
            _defaultCase.SetFactoryMethod(factory);
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
            var defaultCompiler = _defaultCase.Compiler ?? CachingMappingCompiler.GetDefaultInstance();

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
            Assert.ArgumentNotNull(predicate, nameof(predicate));

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
                return r => default(T);
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

                return default(T);
            };
        }

        private void VerifyAndSetupSubclassPredicate(IDataReader reader, IMapCompiler defaultCompiler, SubclassPredicate subclass, List<SubclassPredicate> newSubclasses)
        {
            if (subclass.Type == null)
                throw new Exception("Specified type may not be null");
            if (!typeof(T).IsAssignableFrom(subclass.Type))
                throw new Exception($"Type {subclass.Type.FullName} is not assignable to {typeof(T).FullName}");
            if (subclass.Mapper == null)
            {
                var context = new MapCompileContext<T>(reader, subclass.Type, subclass.Factory, subclass.Constructor, subclass.ConstructorFinder);
                subclass.Mapper = (subclass.Compiler ?? defaultCompiler).CompileExpression(context);
            }

            newSubclasses.Add(subclass);
        }
    }
}