﻿using System;
using System.Collections.Generic;

namespace CastIron.Sql.Mapping
{
    /// <summary>
    /// Holds configuration settings for all types. These are used during compilation to control
    /// how objects are instantiated and mapped
    /// </summary>
    public class TypeSettingsCollection
    {
        private readonly Dictionary<Type, ITypeSettings> _types;
        private readonly ITypeSettings _default;

        public string Separator { get; private set; }
        public ICollection<string> IgnorePrefixes { get; private set; }
        public IMapCompiler DefaultCompiler { get; private set; }

        public TypeSettingsCollection()
        {
            _types = new Dictionary<Type, ITypeSettings>();
            _default = new TypeSettings<object>();
            Separator = "_";
        }

        public void Add(ITypeSettings type)
        {
            _types.Add(type.BaseType, type);
        }

        public ITypeSettings GetTypeSettings(Type type)
        {
            return _types.ContainsKey(type) ? _types[type] : _default;
        }

        public void SetChildSeparator(string separator)
        {
            Separator = separator ?? "_";
        }
    }
}