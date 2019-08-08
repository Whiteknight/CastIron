using System;
using System.Collections.Concurrent;
using System.Data;
using System.Text;
using CastIron.Sql.Utility;

namespace CastIron.Sql.Mapping
{
    public class CachingMappingCompiler : IMapCompiler
    {
        private readonly IMapCompiler _inner;
        private readonly ConcurrentDictionary<string, object> _cache;

        public CachingMappingCompiler(IMapCompiler inner)
        {
            _inner = inner;
            _cache = new ConcurrentDictionary<string, object>();
        }

        public void ClearCache()
        {
            _cache.Clear();
        }

        public Func<IDataRecord, T> CompileExpression<T>(MapCompileContext context)
        {
            Argument.NotNull(context, nameof(context));

            // We cannot cache if a custom factory is provided. The internals of the factory can change
            if (context.Factory != null)
                return _inner.CompileExpression<T>(context);

            var key = CreateKey<T>(context);
            if (_cache.TryGetValue(key, out var cached) && cached is Func<IDataRecord, T> func)
                return func;

            var compiled = _inner.CompileExpression<T>(context);
            _cache.TryAdd(key, compiled);
            return compiled;
        }

        private static string CreateKey<T>(MapCompileContext context)
        {
            var sb = new StringBuilder();
            sb.AppendLine("P:" + typeof(T).FullName);
            sb.AppendLine("S:" + context.Specific.FullName);
            if (context.PreferredConstructor != null)
            {
                sb.Append("C:");
                foreach (var param in context.PreferredConstructor.GetParameters())
                {
                    sb.Append(param.Name);
                    sb.Append(":");
                    sb.Append(param.ParameterType.FullName);
                    sb.Append(",");
                }

                sb.AppendLine();
            }
            for (var i = 0; i < context.Reader.FieldCount; i++)
            {
                sb.Append(i);
                sb.Append(":");
                sb.AppendLine(context.Reader.GetFieldType(i).FullName);
            }
            
            return sb.ToString();
        }
    }
}