using System;
using System.Data;
using CastIron.Sql.Utility;

namespace CastIron.Sql.Mapping
{
    public class MapCompiler : IMapCompiler
    {
        private readonly TupleMapCompiler _tuples;
        private readonly PropertyAndConstructorMapCompiler _constructors;
        private readonly ObjectMapCompiler _objects;
        private readonly PrimitiveMapCompiler _primitives;

        public MapCompiler()
        {
            _tuples = new TupleMapCompiler();
            _constructors = new PropertyAndConstructorMapCompiler();
            _objects = new ObjectMapCompiler();
            _primitives = new PrimitiveMapCompiler();
        }

        public Func<IDataRecord, T> CompileExpression<T>(MapCompileContext<T> context)
        {
            Assert.ArgumentNotNull(context, nameof(context));

            var strategy = GetStrategy(context.Parent, context.Specific, context.Factory);
            return strategy.CompileExpression(context);
        }

        private IMapCompiler GetStrategy(Type parentType, Type specific, object factory)
        {
            if (PrimitiveMapCompiler.IsMatchingType(parentType))
                return _primitives;
            
            // If asked for an object array, or just an object, return those as object arrays and don't do any fancy mapping
            if (ObjectMapCompiler.IsMatchingType(parentType) && ObjectMapCompiler.IsMatchingType(specific))
                return _objects;

            if (TupleMapCompiler.IsMatchingType(parentType, factory))
                return _tuples;

            if (parentType.IsAssignableFrom(specific))
                return _constructors;

            throw new Exception($"Could not find mapper for Type=({(specific ?? parentType).Name} is {parentType.Name})");
        }
    }
}