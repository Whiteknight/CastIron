using System;
using System.Collections.Generic;
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
            if (!IsMatchingType(typeof(T)))
                return r => default(T);
            if (!IsMatchingType(specific))
                return r => default(T);
            return CreateObjectArrayMap(reader) as Func<IDataRecord, T>;
        }

        public static bool IsMatchingType(Type t)
        {
            return t == null || t == typeof(object) || t == typeof(object[]) || t == typeof(IEnumerable<object>) || t == typeof(IList<object>) || t == typeof(IReadOnlyList<object>);
        }
    }
}