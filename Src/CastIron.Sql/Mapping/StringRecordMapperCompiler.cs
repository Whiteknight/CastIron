using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace CastIron.Sql.Mapping
{
    public class StringRecordMapperCompiler : IRecordMapperCompiler
    {
        private static Func<IDataRecord, string[]> CreateStringArrayMap(IDataReader reader)
        {
            var columns = reader.FieldCount;
            return r =>
            {
                var buffer = new string[columns];
                for (var i = 0; i < columns; i++)
                {
                    var objValue = r.GetValue(i);
                    if (!(objValue is DBNull))
                        buffer[i] = objValue.ToString();
                }
                return buffer;
            };
        }

        public Func<IDataRecord, T> CompileExpression<T>(Type specific, IDataReader reader, Func<T> factory, ConstructorInfo preferredConstructor)
        {
            if (!IsMatchingType(typeof(T)))
                return r => default(T);
            if (!IsMatchingType(specific))
                return r => default(T);
            return CreateStringArrayMap(reader) as Func<IDataRecord, T>;
        }

        public static bool IsMatchingType(Type t)
        {
            return t == null || t == typeof(string[]) || t == typeof(IEnumerable<string>) || t == typeof(IList<string>) || t == typeof(IReadOnlyList<string>);
        }
    }
}