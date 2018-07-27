using System;
using System.Collections.Generic;
using System.Data;

namespace CastIron.Sql.Mapping
{
    public static class DefaultMappings
    {
        // TODO: If T is a Tuple type and the value types match, map it by column order
        public static Func<IDataRecord, T> Create<T>(IDataReader reader)
        {
            if (reader == null)
                return r => default(T);

            var type = typeof(T);
            if (type == typeof(object[]))
                return CreateObjectArrayMap(reader) as Func<IDataRecord, T>;
            if (type == typeof(object))
                return CreateObjectArrayMap(reader) as Func<IDataRecord, T>;

            return CreatePropertyAndConstructorMapping<T>(reader);
        }

        private static Func<IDataRecord, T> CreatePropertyAndConstructorMapping<T>(IDataReader reader)
        {
            var columnNames = new Dictionary<string, int>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var name = reader.GetName(i).ToLowerInvariant();
                if (string.IsNullOrEmpty(name))
                    continue;
                if (columnNames.ContainsKey(name))
                    continue;
                columnNames.Add(name, i);
            }

            return PropertyAndConstructorRecordMapperCompiler.CompileExpression<T>(columnNames);
        }

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
    }
}