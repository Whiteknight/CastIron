using System;
using System.Data;
using System.Reflection;

namespace CastIron.Sql
{
    /// <summary>
    /// Mapping object which allows the selection of different subclasses or mappings depending on the data 
    /// in a record 
    /// </summary>
    /// <typeparam name="TParent"></typeparam>
    public interface ISubclassMapping<in TParent>
    {
        /// <summary>
        /// Use a specific subclass when the predicate is satisfied. These predicates are executed in the
        /// order in which they are configured. Once the subclass type is selected, the mapping compiler
        /// will generate a mapping using the given mapping callback, factory method or preferred constructor
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="determine">Predicate to determine if the subclass should be used</param>
        /// <param name="map"></param>
        /// <param name="factory"></param>
        /// <param name="preferredConstructor"></param>
        /// <returns></returns>
        ISubclassMapping<TParent> UseSubclass<T>(Func<IDataRecord, bool> determine, Func<IDataRecord, TParent> map = null, Func<TParent> factory = null, ConstructorInfo preferredConstructor = null)
            where T : TParent;

        /// <summary>
        /// The fallback subclass to use when no predicates are satisfied. The mapping compiler will generate
        /// a mapping using the given mapping callback, factory method or preferred constructor.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="map"></param>
        /// <param name="factory"></param>
        /// <param name="preferredConstructor"></param>
        /// <returns></returns>
        ISubclassMapping<TParent> Otherwise<T>(Func<IDataRecord, TParent> map = null, Func<TParent> factory = null, ConstructorInfo preferredConstructor = null)
            where T : TParent;
    }
}