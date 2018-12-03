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
        private readonly StringRecordMapperCompiler _strings;

        public RecordMapperCompiler()
        {
            _tuples = new TupleRecordMapperCompiler();
            _constructors = new PropertyAndConstructorRecordMapperCompiler();
            _objects = new ObjectRecordMapperCompiler();
            _primitives = new PrimitiveRecordMapperCompiler();
            _strings = new StringRecordMapperCompiler();
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
            if (CompilerTypes.Primitive.Contains(parentType))
                return _primitives;
            
            // If asked for an object array, or just an object, return those as object arrays and don't do any fancy mapping
            if (parentType == typeof(object[]) && specific == typeof(object[]))
                return _objects;
            if (parentType == typeof(object) && specific == typeof(object))
                return _objects;

            // If asked for an array of strings, we can do that too
            if ((parentType == typeof(object) || parentType == typeof(object[]) || parentType == typeof(string[])) && specific == typeof(string[]))
                return _strings;

            if (parentType.Namespace == "System" && parentType.Name.StartsWith("Tuple") && specific == parentType && factory == null && preferredConstructor == null)
                return _tuples;
            if (!parentType.IsAssignableFrom(specific))
                throw new Exception($"Type {specific.Name} must be assignable to {parentType.Name}.Name");
            return _constructors;
        }
    }
}