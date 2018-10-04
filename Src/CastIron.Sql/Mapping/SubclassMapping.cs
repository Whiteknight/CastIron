using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace CastIron.Sql.Mapping
{
    public class SubclassMapping<TParent> : ISubclassMapping<TParent>
    {
        private readonly IRecordMapperCompiler _inner;
        private readonly List<SubclassPredicate> _subclasses;
        private readonly SubclassPredicate _otherwise;
        private bool _calledOtherwise;

        public SubclassMapping(IRecordMapperCompiler inner = null)
        {
            _inner = inner ?? CachingMappingCompiler.GetDefaultInstance();
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
        }

        public ISubclassMapping<TParent> UseSubclass<T>(Func<IDataRecord, bool> determine)
            where T : TParent
        {
            if (typeof(T).IsAbstract)
                throw new Exception("Type must be concrete");
            _subclasses.Add(new SubclassPredicate
            {
                Type = typeof(T),
                Predicate = determine ?? (r => true)
            });
            return this;
        }

        public ISubclassMapping<TParent> Otherwise<T>()
            where T : TParent
        {
            if (_calledOtherwise)
                throw new Exception($".{nameof(Otherwise)}() method can be called at most once.");
            _otherwise.Type = typeof(T);
            _calledOtherwise = true;
            return this;
        }

        public Func<IDataRecord, TParent> BuildThunk(IDataReader reader)
        {
            var fallback = _otherwise?.Type ?? typeof(TParent);
            if (fallback.IsAbstract || fallback.IsInterface)
                throw new Exception("Fallback class must be instantiable");
            
            // 1. Compile a mapper for every possible subclass
            var mappers = _subclasses
                .Select(sc => sc.Type)
                .Concat(new [] {  _otherwise?.Type })
                .Where(t => t != null)
                .Where(t => typeof(TParent).IsAssignableFrom(t))
                .Distinct()
                .ToDictionary(t => t, t => _inner.CompileExpression<TParent>(t, reader, null));

            // 2. Create a thunk which checks each predicate and calls the correct mapper
            return (r =>
            {
                foreach (var subclass in _subclasses)
                {
                    if (!subclass.Predicate(r))
                        continue;
                    if (!mappers.ContainsKey(subclass.Type))
                        continue;
                    var map = mappers[subclass.Type];
                    if (map == null)
                        continue;
                    var result = map(r);
                    
                    return (TParent)((object)result);
                }

                var otherwiseMap = mappers[_otherwise.Type];
                if (otherwiseMap == null)
                    return default(TParent);
                var otherwiseResult = otherwiseMap(r);
                return (TParent) ((object) otherwiseResult);
            });
            //var readerParam = Expression.Parameter(typeof(IDataRecord), "record");
            //var returnTarget = Expression.Label("return");
            //var expressions = new List<Expression>();
            //foreach (var subclass in _subclasses)
            //{
            //    var mapped = mappers[subclass.Type];
            //    Expression<Func<IDataRecord, TParent>> map = (r => mapped(r));
            //    expressions.Add(
            //        Expression.IfThen(
            //            Expression.Invoke(
            //                subclass.Predicate, 
            //                readerParam),
            //            Expression.Return(
            //                returnTarget,
            //                Expression.Convert(
            //                    Expression.Invoke(map, readerParam), 
            //                    typeof(T)))));
            //}

            //if (_otherwise?.Type == null || !mappers.ContainsKey(_otherwise.Type))
            //    expressions.Add(Expression.Return(returnTarget, Expression.Convert(Expression.Constant(null), typeof(T))));
            //else
            //{
            //    var mapped = mappers[_otherwise.Type];
            //    Expression<Func<IDataRecord, TParent>> map = (r => mapped(r));
            //    expressions.Add(
            //        Expression.Return(
            //            returnTarget, 
            //            Expression.Convert(
            //                Expression.Invoke(map, readerParam),
            //                typeof(T))));
            //}

            //expressions.Add(Expression.Label(returnTarget));
            //var s = string.Join("\n", expressions.Select(e => e.ToString()));
            //var lambda =  Expression.Lambda<Func<IDataRecord, T>>(
            //        Expression.Block(typeof(T), new[] { readerParam }, expressions));
            //var s = lambda.ToString();
            //return lambda.Compile();
        }
    }
}