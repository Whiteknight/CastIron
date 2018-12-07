using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace CastIron.Sql.Mapping
{
    public class ObjectRecordMapperCompiler : IRecordMapperCompiler
    {
        public Func<IDataRecord, T> CompileExpression<T>(Type specific, IDataReader reader, Func<T> factory, ConstructorInfo preferredConstructor)
        {
            if (!IsMatchingType(typeof(T)))
                return r => default(T);
            if (!IsMatchingType(specific))
                return r => default(T);
            var columns = reader.FieldCount;
            if (columns == 1 && typeof(T) == typeof(object) && specific == typeof(object))
                return CreateObjectMap() as Func<IDataRecord, T>;
            return CreateObjectArrayMap(columns) as Func<IDataRecord, T>;
        }

        public static bool IsMatchingType(Type t)
        {
            return t == null 
                || t == typeof(object) || t == typeof(object[]) 
                || t == typeof(IEnumerable<object>) || t == typeof(IList<object>) || t == typeof(IReadOnlyList<object>) || t == typeof(ICollection<object>)
                || t == typeof(IEnumerable) || t == typeof(IList) || t == typeof(ICollection);
        }

        private static Func<IDataRecord, object> CreateObjectMap()
        {
            return r => r.GetValue(0);
        }

        private static Func<IDataRecord, object[]> CreateObjectArrayMap(int numColumns)
        {
            return r =>
            {
                var buffer = new object[numColumns];
                r.GetValues(buffer);
                return buffer;
            };
        }
    }
}