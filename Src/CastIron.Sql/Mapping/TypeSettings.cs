using System;
using System.Collections.Generic;
using System.Linq;

namespace CastIron.Sql.Mapping
{
    /// <summary>
    /// Used during compilation to get settings related to a type. During compilation, the type
    /// instantiated may be this type or a subtype
    /// </summary>
    public interface ITypeSettings
    {
        Type BaseType { get; }
        ISpecificTypeSettings GetDefault();
        IEnumerable<ISpecificTypeSettings> GetSpecificTypes();
    }

    public class TypeSettings<T> : ITypeSettings
    {
        private readonly List<ISpecificTypeSettings<T>> _subclasses;

        public TypeSettings()
        {
            Default = new SpecificTypeSettings<T, T>
            {
                Predicate = r => true,
                Type = typeof(T)
            };
            BaseType = typeof(T);
            _subclasses = new List<ISpecificTypeSettings<T>>();
        }

        public void AddType(ISpecificTypeSettings<T> specific)
        {
            _subclasses.Add(specific);
        }

        public ISpecificTypeSettings GetDefault() => Default;
        public ISpecificTypeSettings<T> Default { get; private set; }

        public Type BaseType { get; }

        public IEnumerable<ISpecificTypeSettings> GetSpecificTypes() => _subclasses ?? Enumerable.Empty<ISpecificTypeSettings>();
    }
}