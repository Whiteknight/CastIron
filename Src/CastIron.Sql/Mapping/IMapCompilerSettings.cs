using System;
using System.Data;
using System.Reflection;

namespace CastIron.Sql.Mapping
{
    public interface IMapCompilerSettings
    {
        IMapCompilerSettings For<T>(Action<IMapCompilerSettings<T>> setup);
        IMapCompilerSettings UseChildSeparator(string separator);

        // allSettings -> Type[] -> Subtype -> options

        // TODO: Set ignorePrefixes and separators here?
    }

    public interface IMapCompilerSettingsBase<in T>
    {
        /// <summary>
        /// Use a given mapping function and do not compile anything
        /// </summary>
        /// <param name="map"></param>
        /// <returns></returns>
        IMapCompilerSettingsBase<T> UseMap(Func<IDataRecord, T> map);

        /// <summary>
        /// Use the specified constructor to create the object
        /// </summary>
        /// <param name="constructor"></param>
        /// <returns></returns>
        IMapCompilerSettingsBase<T> UseConstructor(ConstructorInfo constructor);

        /// <summary>
        /// Use the constructor which takes the given argument list to create the object
        /// </summary>
        /// <param name="argumentTypes"></param>
        /// <returns></returns>
        IMapCompilerSettingsBase<T> UseConstructor(params Type[] argumentTypes);

        /// <summary>
        /// Use the specified constructor finder to find the most appropriate constructor to use
        /// to build the object
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        IMapCompilerSettings<T> UseConstructorFinder(IConstructorFinder finder);

        /// <summary>
        /// Use a factory method to create the object
        /// </summary>
        /// <param name="factory"></param>
        /// <returns></returns>
        IMapCompilerSettingsBase<T> UseFactoryMethod(Func<T> factory);

    }

    /// <summary>
    /// Options to compile a mapping from data reader to a specified type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IMapCompilerSettings<in T> : IMapCompilerSettingsBase<T>
    {
        /// <summary>
        /// Use a specific type when trying to instantiate the requested type T. The specific type 
        /// must be T or a subclass of T
        /// </summary>
        /// <typeparam name="TSpecific"></typeparam>
        /// <returns></returns>
        IMapCompilerSettings<T> UseClass<TSpecific>()
            where TSpecific : T;

        /// <summary>
        /// Specify a condition where a specific subclass of T should be constructed instead of
        /// T
        /// </summary>
        /// <typeparam name="TSubclass"></typeparam>
        /// <param name="predicate"></param>
        /// <param name="setup"></param>
        /// <returns></returns>
        IMapCompilerSettings<T> UseSubclass<TSubclass>(Func<IDataRecord, bool> predicate, Action<IMapCompilerSettingsBase<T>> setup = null)
            where TSubclass : T;
    }
}