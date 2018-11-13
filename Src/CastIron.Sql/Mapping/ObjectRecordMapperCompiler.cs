using System;
using System.Data;
using System.Reflection;

namespace CastIron.Sql.Mapping
{
    public class ObjectRecordMapperCompiler : IRecordMapperCompiler
    {
        private static Func<IDataRecord, object[]> CreateObjectArrayMap(IDataReader reader)
        {
            var columns = reader.FieldCount;
            return r =>
            {
                var buffer = new object[columns];
                r.GetValues(buffer);
                return buffer;
            };
        }

        public Func<IDataRecord, T> CompileExpression<T>(Type specific, IDataReader reader, Func<T> factory, ConstructorInfo preferredConstructor)
        {
            return CreateObjectArrayMap(reader) as Func<IDataRecord, T>;
        }
    }
}