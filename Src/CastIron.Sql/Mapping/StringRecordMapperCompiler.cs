using System;
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
            return CreateStringArrayMap(reader) as Func<IDataRecord, T>;
        }
    }
}