using System;
using System.Collections.Concurrent;
using System.Data;
using System.Reflection;
using System.Text;
using System.Threading;

namespace CastIron.Sql.Mapping
{
    public class CachingMappingCompiler : IRecordMapperCompiler
    {
        private readonly IRecordMapperCompiler _inner;
        private readonly ConcurrentDictionary<string, object> _cache;

        private static CachingMappingCompiler _defaultInstance;

        public CachingMappingCompiler(IRecordMapperCompiler inner)
        {
            _inner = inner;
            _cache = new ConcurrentDictionary<string, object>();
        }

        public static CachingMappingCompiler GetDefaultInstance()
        {
            if (_defaultInstance != null)
                return _defaultInstance;

            var newInstance = new CachingMappingCompiler(new PropertyAndConstructorRecordMapperCompiler());
            var oldValue = Interlocked.CompareExchange(ref _defaultInstance, newInstance, null);
            return oldValue ?? newInstance;
        }

        public void ClearCache()
        {
            _cache.Clear();
        }

        public Func<IDataRecord, T> CompileExpression<T>(IDataReader reader)
        {
            CIAssert.ArgumentNotNull(reader, nameof(reader));

            var key = CreateKey<T>(typeof(T), reader, null);
            if (_cache.TryGetValue(key, out object cached) && cached is Func<IDataRecord, T> func)
                return func;

            var compiled = _inner.CompileExpression<T>(reader);
            _cache.TryAdd(key, compiled);
            return compiled;
        }

        public Func<IDataRecord, T> CompileExpression<T>(Type specific, IDataReader reader, Func<T> factory, ConstructorInfo preferredConstructor)
        {
            CIAssert.ArgumentNotNull(specific, nameof(specific));
            CIAssert.ArgumentNotNull(reader, nameof(reader));

            // We cannot cache if a custom factory is provided. The internals of the factory can change
            if (factory != null)
                return _inner.CompileExpression(specific, reader, factory, preferredConstructor);

            var key = CreateKey<T>(specific, reader, null);
            if (_cache.TryGetValue(key, out object cached) && cached is Func<IDataRecord, T> func)
                return func;

            var compiled = _inner.CompileExpression<T>(specific, reader, null, preferredConstructor);
            _cache.TryAdd(key, compiled);
            return compiled;
        }

        private static string CreateKey<T>(Type specific, IDataReader reader, ConstructorInfo preferredConstructor)
        {
            var sb = new StringBuilder();
            sb.AppendLine("P:" + typeof(T).FullName);
            sb.AppendLine("S:" + specific.FullName);
            if (preferredConstructor != null)
            {
                sb.Append("C:");
                foreach (var param in preferredConstructor.GetParameters())
                {
                    sb.Append(param.Name);
                    sb.Append(":");
                    sb.Append(param.ParameterType.FullName);
                    sb.Append(",");
                }

                sb.AppendLine();
            }
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