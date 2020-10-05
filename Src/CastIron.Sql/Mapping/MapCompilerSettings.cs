using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using CastIron.Sql.Utility;

namespace CastIron.Sql.Mapping
{
    /// <summary>
    /// Facade object to build up TypeSettingsCollection
    /// </summary>
    public class MapCompilerSettings : IMapCompilerSettings
    {
        private readonly CompilationSettings _types;

        public MapCompilerSettings(CompilationSettings types)
        {
            _types = types;
        }

        public IMapCompilerSettings For<T>(Action<IMapCompilerSettings<T>> setup)
        {
            var builder = new MapCompilerSettings<T>(_types);
            setup?.Invoke(builder);
            return this;
        }

        public IMapCompilerSettings IgnorePrefixes(IEnumerable<string> prefixes)
        {
            var prefixLookup = new HashSet<string>(prefixes.Distinct());
            _types.SetIgnorePrefixes(prefixLookup);
            return this;
        }

        public IMapCompilerSettings UseChildSeparator(string separator)
        {
            _types.SetChildSeparator(separator);
            return this;
        }
    }

    /// <summary>
    /// A facade to build up TypeSettings<T> using IMapCompilerSettings<T> interface
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MapCompilerSettings<T> : IMapCompilerSettings<T>
    {
        private readonly ISpecificTypeSettings<T> _specific;
        private readonly CompilationSettings _types;
        private readonly TypeSettings<T> _currentType;

        public MapCompilerSettings(CompilationSettings types)
        {
            _types = types;
            _currentType = new TypeSettings<T>();
            _types.Add(_currentType);
            _specific = _currentType.Default;
        }

        private MapCompilerSettings(CompilationSettings types, TypeSettings<T> type, ISpecificTypeSettings<T> specific)
        {
            _types = types;
            _currentType = type;
            _specific = specific;
        }

        public IMapCompilerSettingsBase<T> UseMap(Func<IDataRecord, T> map)
        {
            Argument.NotNull(map, nameof(map));
            _specific.SetMap(map);
            return this;
        }

        public IMapCompilerSettingsBase<T> UseConstructor(ConstructorInfo constructor)
        {
            Argument.NotNull(constructor, nameof(constructor));
            _specific.SetConstructor(constructor);
            return this;
        }

        public IMapCompilerSettingsBase<T> UseConstructor(params Type[] argumentTypes)
        {
            var constructor = typeof(T).GetConstructor(argumentTypes) ?? throw ConstructorFindException.NoSuitableConstructorFound(typeof(T));
            return UseConstructor(constructor);
        }

        public IMapCompilerSettings<T> UseConstructorFinder(IConstructorFinder finder)
        {
            Argument.NotNull(finder, nameof(finder));
            _specific.SetConstructorFinder(finder);
            return this;
        }

        public IMapCompilerSettingsBase<T> UseFactoryMethod(Func<T> factory)
        {
            Argument.NotNull(factory, nameof(factory));
            _specific.SetFactoryMethod(factory);
            return this;
        }

        public IMapCompilerSettings<T> UseType<TSpecific>()
            where TSpecific : T
        {
            // TSpecific must be a constructable, public class
            var type = typeof(TSpecific);
            if (type.IsInterface || type.IsAbstract)
                throw MapCompilerException.UnconstructableAbstractType(type);
            _specific.SetType(typeof(TSpecific));
            return this;
        }

        public IMapCompilerSettings<T> UseSubtype<TSubtype>(Func<IDataRecord, bool> predicate, Action<IMapCompilerSettingsBase<T>> setup = null)
            where TSubtype : T
        {
            Argument.NotNull(predicate, nameof(predicate));

            var subclassType = typeof(TSubtype);
            if (subclassType.IsInterface || subclassType.IsAbstract)
                throw MapCompilerException.UnconstructableAbstractType(subclassType);

            var predicateContext = new SpecificTypeSettings<T, TSubtype>();
            predicateContext.SetType(typeof(TSubtype));
            predicateContext.Predicate = predicate;

            if (setup != null)
            {
                var specific = new SpecificTypeSettings<T, TSubtype>();
                _currentType.AddType(specific);
                var subclassContext = new MapCompilerSettings<T>(_types, _currentType, specific);
                setup.Invoke(subclassContext);
            }
            _currentType.AddType(predicateContext);
            return this;
        }
    }
}