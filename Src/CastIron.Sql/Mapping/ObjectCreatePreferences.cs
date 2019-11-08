using System;
using System.Collections.Generic;
using System.Reflection;

namespace CastIron.Sql.Mapping
{
    /// <summary>
    /// Holds preferences about how to construct objects of different types
    /// </summary>
    public class ObjectCreatePreferences
    {
        private readonly Dictionary<Type, SingleTypePreferences> _types;
        private static readonly SingleTypePreferences _default = new SingleTypePreferences(typeof(object), null, null);
        
        public IConstructorFinder ConstructorFinder { get; }

        public ObjectCreatePreferences(IConstructorFinder constructorFinder)
        {
            _types = new Dictionary<Type, SingleTypePreferences>();
            ConstructorFinder = constructorFinder;
        }

        public class SingleTypePreferences
        {
            public SingleTypePreferences(Type type, Func<object> factory, ConstructorInfo preferredConstructor)
            {
                Type = type;
                Factory = factory;
                PreferredConstructor = preferredConstructor;
            }

            public Type Type { get; set; }
            public Func<object> Factory { get; }
            public ConstructorInfo PreferredConstructor { get; }
        }

        public void AddType(Type type, Func<object> factory, ConstructorInfo preferredConstructor)
        {
            _types.Add(type, new SingleTypePreferences(type, factory, preferredConstructor));
        }

        public ConstructorInfo GetConstructor(IProviderConfiguration provider, IReadOnlyDictionary<string, int> columnNameCounts, Type type)
        {
            var specificType = _types.ContainsKey(type) ? _types[type] : _default;
            var finder = ConstructorFinder ?? BestMatchConstructorFinder.GetDefaultInstance();
            return finder.FindBestMatch(provider, specificType.PreferredConstructor, type, columnNameCounts);
        }

        public ConstructorInfo GetPreferredConstructor(Type type) 
            => _types.ContainsKey(type) ? _types[type].PreferredConstructor : null;

        public Func<object> GetFactory(Type type) => _types.ContainsKey(type) ? _types[type].Factory : null;
    }
}