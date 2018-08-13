using System;
using System.Collections.Concurrent;
using System.Data;
using System.Text;

namespace CastIron.Sql.Mapping
{
    public class CachingMappingCompiler : IRecordMapperCompiler
    {
        private readonly IRecordMapperCompiler _inner;
        private readonly ConcurrentDictionary<string, object> _cache;

        public CachingMappingCompiler(IRecordMapperCompiler inner)
        {
            _inner = inner;
            _cache = new ConcurrentDictionary<string, object>();
        }

        public Func<IDataRecord, T> CompileExpression<T>(IDataReader reader)
        {
            var key = CreateKey<T>(reader);
            if (_cache.TryGetValue(key, out object cached) && cached is Func<IDataRecord, T> func)
                return func;

            var compiled = _inner.CompileExpression<T>(reader);
            _cache.TryAdd(key, compiled);
            return compiled;
        }

        private static string CreateKey<T>(IDataReader reader)
        {
            var sb = new StringBuilder();
            sb.AppendLine(typeof(T).FullName);
            for (int i = 0; i < reader.FieldCount; i++)
            {
                sb.Append(i);
                sb.Append(":");
                sb.AppendLine(reader.GetFieldType(i).FullName);
            }

            return sb.ToString();
        }
    }
}