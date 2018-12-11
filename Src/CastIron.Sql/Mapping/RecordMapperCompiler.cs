using System;
using System.Data;
using System.Reflection;
using CastIron.Sql.Utility;

namespace CastIron.Sql.Mapping
{
    public class RecordMapperCompiler : IRecordMapperCompiler
    {
        private readonly TupleRecordMapperCompiler _tuples;
        private readonly PropertyAndConstructorRecordMapperCompiler _constructors;
        private readonly ObjectRecordMapperCompiler _objects;
        private readonly PrimitiveRecordMapperCompiler _primitives;

        public RecordMapperCompiler()
        {
            _tuples = new TupleRecordMapperCompiler();
            _constructors = new PropertyAndConstructorRecordMapperCompiler();
            _objects = new ObjectRecordMapperCompiler();
            _primitives = new PrimitiveRecordMapperCompiler();
        }

        public Func<IDataRecord, T> CompileExpression<T>(Type specific, IDataReader reader, Func<T> factory, ConstructorInfo preferredConstructor)
        {
            Assert.ArgumentNotNull(reader, nameof(reader));
            specific = specific ?? typeof(T);

            var parentType = typeof(T);
            var strategy = GetStrategy(parentType, specific, factory, preferredConstructor);
            return strategy.CompileExpression(specific, reader, factory, preferredConstructor);
        }

        private IRecordMapperCompiler GetStrategy(Type parentType, Type specific, object factory, object preferredConstructor)
        {
            if (PrimitiveRecordMapperCompiler.IsMatchingType(parentType))
                return _primitives;
            
            // If asked for an object array, or just an object, return those as object arrays and don't do any fancy mapping
            if (ObjectRecordMapperCompiler.IsMatchingType(parentType) && ObjectRecordMapperCompiler.IsMatchingType(specific))
                return _objects;

            if (TupleRecordMapperCompiler.IsMatchingType(parentType, factory, preferredConstructor))
                return _tuples;

            if (parentType.IsAssignableFrom(specific))
                return _constructors;

            throw new Exception($"Could not find mapper for Type=({(specific ?? parentType).Name} is {parentType.Name})");
        }
    }
}