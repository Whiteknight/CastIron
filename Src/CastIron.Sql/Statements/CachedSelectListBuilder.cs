using System;
using System.Collections.Generic;

namespace CastIron.Sql.Statements
{
    public class CachedSelectListBuilder : ISelectListBuilder
    {
        private readonly Dictionary<string, string> _cache;
        private readonly ISelectListBuilder _inner;

        public CachedSelectListBuilder(ISelectListBuilder inner)
        {
            _inner = inner;
            _cache = new Dictionary<string, string>();
        }

        public string BuildSelectList(Type type, string prefix = "")
        {
            var key = $"{prefix}:{type.FullName}";
            if (_cache.ContainsKey(key))
                return _cache[key];

            var list = _inner.BuildSelectList(type);
            _cache.Add(key, list);
            return list;
        }
    }
}