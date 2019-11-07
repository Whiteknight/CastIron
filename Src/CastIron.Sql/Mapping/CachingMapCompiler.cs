using System;
using System.Collections.Concurrent;
using System.Data;
using System.Text;
using CastIron.Sql.Utility;

namespace CastIron.Sql.Mapping
{
    public class CachingMapCompiler : IMapCompiler
    {
        private readonly IMapCompiler _inner;
        private readonly ConcurrentDictionary<string, object> _cache;

        public CachingMapCompiler(IMapCompiler inner)
        {
            _inner = inner;
            _cache = new ConcurrentDictionary<string, object>();
        }

        public void ClearCache()
        {
            _cache.Clear();
        }

        public Func<IDataRecord, T> CompileExpression<T>(MapCompileContext context, IDataReader reader)
        {
            Argument.NotNull(context, nameof(context));

            // We cannot cache if a custom factory is provided. The internals of the factory can change
            var factory = context.GetFactoryMethod(context.Specific);
            if (factory != null)
                return _inner.CompileExpression<T>(context, reader);

            var key = CreateKey<T>(context, reader);
            if (_cache.TryGetValue(key, out var cached) && cached is Func<IDataRecord, T> func)
                return func;

            var compiled = _inner.CompileExpression<T>(context, reader);
            _cache.TryAdd(key, compiled);
            return compiled;
        }

        private static string CreateKey<T>(MapCompileContext context, IDataReader reader)
        {
            var sb = new StringBuilder();
            sb.AppendLine("P:" + typeof(T).FullName);
            sb.AppendLine("S:" + context.Specific.FullName);
            var preferredConstructor = context.GetPreferredConstructor(context.Specific);
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
            for (var i = 0; i < reader.FieldCount; i++)
            {
                sb.Append(i);
                sb.Append(":");
                sb.AppendLine(reader.GetFieldType(i).FullName);
            }
            
            return sb.ToString();
        }
    }
}