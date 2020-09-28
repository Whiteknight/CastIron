using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using CastIron.Sql.Utility;

namespace CastIron.Sql.Mapping
{
    public interface IMapCompilerSettings
    {
        /// <summary>
        /// Configure a single type. Whenever the compiler sees this exact type, it will use these
        /// configured options.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="setup"></param>
        /// <returns></returns>
        IMapCompilerSettings For<T>(Action<IMapCompilerSettings<T>> setup);

        /// <summary>
        /// Use a specific separator string to separate parent property names from child propery
        /// names. By default "_" is used
        /// </summary>
        /// <param name="separator"></param>
        /// <returns></returns>
        IMapCompilerSettings UseChildSeparator(string separator);

        /// <summary>
        /// Set a list of prefixes to ignore when matching column names to property names. If the
        /// column name starts with one of these prefixes, the prefix is removed before matching.
        /// A name 'Table_ID' with ignored prefix 'Table_' becomes 'ID'
        /// </summary>
        /// <param name="prefixes"></param>
        /// <returns></returns>
        IMapCompilerSettings IgnorePrefixes(IEnumerable<string> prefixes);
    }

    public static class MapCompilerExtensions
    {
        /// <summary>
        /// Set a list of prefixes to ignore when matching column names to property names. If the
        /// column name starts with one of these prefixes, the prefix is removed before matching.
        /// A name 'Table_ID' with ignored prefix 'Table_' becomes 'ID'
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="prefixes"></param>
        /// <returns></returns>
        public static IMapCompilerSettings IgnorePrefixes(this IMapCompilerSettings settings, params string[] prefixes)
        {
            Argument.NotNull(settings, nameof(settings));
            return settings.IgnorePrefixes(prefixes);
        }
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