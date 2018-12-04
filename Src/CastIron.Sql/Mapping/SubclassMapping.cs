using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace CastIron.Sql.Mapping
{
    public class SubclassMapping<TParent> : ISubclassMapping<TParent>
    {
        private readonly List<SubclassPredicate> _subclasses;
        private readonly SubclassPredicate _otherwise;
        private bool _calledOtherwise;

        public SubclassMapping()
        {
            _subclasses = new List<SubclassPredicate>();
            _otherwise = new SubclassPredicate
            {
                Predicate = r => true,
                Type = !typeof(TParent).IsAbstract ? typeof(TParent) : null
            };
        }

        private class SubclassPredicate
        {
            public Type Type { get; set; }
            public Func<IDataRecord, bool> Predicate { get; set; }
            public IRecordMapperCompiler Compiler { get; set; }
            public Func<TParent> Factory { get; set; }
            public ConstructorInfo Constructor { get; set; }
            public Func<IDataRecord, TParent> Mapper { get; set; }
        }

        private void AssertCorrectCreationArguments(Func<IDataRecord, TParent> map, Func<TParent> factory, ConstructorInfo preferredConstructor)
        {
            int notNulls = 0;
            notNulls += map == null ? 0 : 1;
            notNulls += factory == null ? 0 : 1;
            notNulls += preferredConstructor == null ? 0 : 1;
            if (notNulls > 1)
                throw new Exception($"At most one of {nameof(map)}, {nameof(factory)} or {nameof(preferredConstructor)} may be specified. The others must be null");
        }

        public Func<IDataRecord, TParent> BuildThunk(IDataReader reader)
        {
            var fallback = _otherwise?.Type ?? typeof(TParent);
            if (fallback.IsAbstract || fallback.IsInterface)
                throw new Exception("Fallback class must be instantiable");
            
            // 1. Compile a mapper for every possible subclass and creation options combo
            // The cache will prevent recompilation of same maps so we won't worry about .Distinct() here.
            var newSubclasses = new List<SubclassPredicate>();
            foreach (var subclass in _subclasses)
                VerifyAndSetupSubclassPredicate(reader, subclass, newSubclasses);
            VerifyAndSetupSubclassPredicate(reader, _otherwise, newSubclasses);

            // 2. Create a thunk which checks each predicate and calls the correct mapper
            return CreateThunkExpression(newSubclasses);
        }

        private static Func<IDataRecord, TParent> CreateThunkExpression(List<SubclassPredicate> subclasses)
        {
            return r =>
            {
                foreach (var subclass in subclasses)
                {
                    if (!subclass.Predicate(r))
                        continue;
                    if (subclass.Mapper == null)
                        continue;
                    var result = subclass.Mapper(r);

                    return (TParent) ((object) result);
                }

                return default(TParent);
            };
        }

        private void VerifyAndSetupSubclassPredicate(IDataReader reader, SubclassPredicate subclass, List<SubclassPredicate> newSubclasses)
        {
            if (subclass.Type == null)
                throw new Exception("Specified type may not be null");
            if (!typeof(TParent).IsAssignableFrom(subclass.Type))
                throw new Exception($"Type {subclass.Type.FullName} is not assignable to {typeof(TParent).FullName}");
            if (subclass.Mapper == null)
                subclass.Mapper = (subclass.Compiler ?? CachingMappingCompiler.GetDefaultInstance()).CompileExpression(subclass.Type, reader, subclass.Factory, subclass.Constructor);
            newSubclasses.Add(subclass);
        }

        public ISubclassMapping<TParent> UseSubclass<T>(Func<IDataRecord, bool> determine, Func<IDataRecord, TParent> map = null) where T : TParent
        {
            if (typeof(T).IsAbstract)
                throw new Exception("Type must be concrete");
            AssertCorrectCreationArguments(map, null, null);
            _subclasses.Add(new SubclassPredicate
            {
                Type = typeof(T),
                Predicate = determine ?? (r => true),
                Mapper = map
            });
            return this;
        }

        public ISubclassMapping<TParent> UseSubclass<T>(Func<IDataRecord, bool> determine, IRecordMapperCompiler compiler, Func<TParent> factory = null, ConstructorInfo preferredConstructor = null) where T : TParent
        {
            if (typeof(T).IsAbstract)
                throw new Exception("Type must be concrete");
            AssertCorrectCreationArguments(null, factory, preferredConstructor);
            _subclasses.Add(new SubclassPredicate
            {
                Type = typeof(T),
                Predicate = determine ?? (r => true),
                Factory = factory,
                Constructor = preferredConstructor
            });
            return this;
        }

        public ISubclassMapping<TParent> Otherwise<T>(Func<IDataRecord, TParent> map = null) where T : TParent
        {
            if (_calledOtherwise)
                throw new Exception($".{nameof(Otherwise)}() method can be called at most once.");
            if (typeof(T).IsAbstract)
                throw new Exception("Type must be concrete");
            AssertCorrectCreationArguments(map, null, null);

            _otherwise.Type = typeof(T);
            _otherwise.Mapper = map;
            _calledOtherwise = true;
            return this;
        }

        public ISubclassMapping<TParent> Otherwise<T>(IRecordMapperCompiler compiler, Func<TParent> factory = null, ConstructorInfo preferredConstructor = null) where T : TParent
        {
            if (_calledOtherwise)
                throw new Exception($".{nameof(Otherwise)}() method can be called at most once.");
            if (typeof(T).IsAbstract)
                throw new Exception("Type must be concrete");
            AssertCorrectCreationArguments(null, factory, preferredConstructor);

            _otherwise.Type = typeof(T);
            _otherwise.Compiler = compiler;
            _otherwise.Constructor = preferredConstructor;
            _otherwise.Factory = factory;
            _calledOtherwise = true;
            return this;
        }

        public ISubclassMapping<TParent> OtherwiseDefault()
        {
            if (_calledOtherwise)
                throw new Exception($".{nameof(Otherwise)}() method can be called at most once.");            

            _otherwise.Type = typeof(TParent);
            _otherwise.Mapper = r => default(TParent);
            _calledOtherwise = true;
            return this;
        }
    }
}