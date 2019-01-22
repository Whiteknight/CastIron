using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using CastIron.Sql.Mapping;
using CastIron.Sql.Utility;

namespace CastIron.Sql
{
    /// <summary>
    /// Combined results from the completed database interaction. Acts as a wrapper around IDataReader and 
    /// the output parameters (if any) from the IDbCommand.
    /// </summary>
    public interface IDataResults
    {
        /// <summary>
        /// Where available, the number of rows affected by the query
        /// </summary>
        int RowsAffected { get; }

        /// <summary>
        /// Get a reference to the raw IDataReader. Accessing the underlying reader consumes it and makes
        /// other methods on this object unusable
        /// </summary>
        /// <returns></returns>
        IDataReader AsRawReader();

        /// <summary>
        /// Returns a reference to the raw IDataReader, decorated with better error messages for debugging
        /// purposes. Accessing the underlying reader consumes it and makes other methods on this object
        /// unusable
        /// </summary>
        /// <returns></returns>
        IDataReader AsRawReaderWithBetterErrorMessages();

        /// <summary>
        /// Map the result set to objects
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="setup">Setup the mapper or mapper compiler using a fluent interface</param>
        /// <returns></returns>
        IEnumerable<T> AsEnumerable<T>(Action<IMapCompilerBuilder<T>> setup = null);

        /// <summary>
        /// Get the value of the output parameter by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        object GetOutputParameterValue(string name);

        /// <summary>
        /// Get the value of the output parameter by name, coerced to the specified type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        T GetOutputParameter<T>(string name);

        /// <summary>
        /// Get the value of the output parameter by name, coerced to the specified type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        T GetOutputParameterOrThrow<T>(string name);

        /// <summary>
        /// Map all output parameter values to the specified object type, where each parameter value is set
        /// to a property of the same name, case-insensitive.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T GetOutputParameters<T>()
            where T : class, new();

        /// <summary>
        /// Advance to the next result set in the reader, throws an exception if there is no result set
        /// </summary>
        /// <returns></returns>
        IDataResults AdvanceToNextResultSet();

        /// <summary>
        /// Advance to the result set in the reader by number, starting with the first result set as 1, 
        /// second result set as 2, etc. Result sets may only be accessed in order, though they can be
        /// skipped. Throws an exception if the specified result set number is lower than the current result
        /// set number, or if the result set with the given number does not exist.
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        IDataResults AdvanceToResultSet(int num);

        /// <summary>
        /// Try to advance to the next result set in the reader. Return true if successful, false otherwise.
        /// </summary>
        /// <returns></returns>
        bool TryAdvanceToNextResultSet();

        /// <summary>
        /// Try to advance to the result set in the reader by number, starting with the first result set as
        /// 1, the second result set as 2, etc. Result sets may only be accessed in order, though they can
        /// be skipped. Returns false if attempting to access a previous result set or if the specified
        /// result set does not exist.
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        bool TryAdvanceToResultSet(int num);
    }

    public interface IDataResultsStream : IDataResults, IDisposable
    {
    }

    /// <summary>
    /// Common extension methods for IDataResults
    /// </summary>
    public static class DataResultsExtensions
    {
        /// <summary>
        /// Map the current result set to an enumerable of objects, using a factory method to create 
        /// instances.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="results"></param>
        /// <param name="factory"></param>
        /// <returns></returns>
        public static IEnumerable<T> AsEnumerable<T>(this IDataResults results, Func<T> factory)
        {
            Assert.ArgumentNotNull(results, nameof(results));
            return results.AsEnumerable<T>(b => b.UseFactoryMethod(factory));
        }

        /// <summary>
        /// Map the current result set to an enumerable of objects, using the specified constructor to
        /// create the instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="results"></param>
        /// <param name="preferredConstructor"></param>
        /// <returns></returns>
        public static IEnumerable<T> AsEnumerable<T>(this IDataResults results, ConstructorInfo preferredConstructor)
        {
            Assert.ArgumentNotNull(results, nameof(results));
            return results.AsEnumerable<T>(b => b.UseConstructor(preferredConstructor));
        }

        /// <summary>
        /// Advance to the next result set and map it to an enumerable of objects using the given factory 
        /// method.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="results"></param>
        /// <param name="factory"></param>
        /// <returns></returns>
        public static IEnumerable<T> GetNextEnumerable<T>(this IDataResults results, Func<T> factory)
        {
            return GetNextEnumerable<T>(results, b => b.UseFactoryMethod(factory));
        }

        /// <summary>
        /// Advance to the next result set and map it to an enumerable of objects using the given constructor
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="results"></param>
        /// <param name="preferredConstructor"></param>
        /// <returns></returns>
        public static IEnumerable<T> GetNextEnumerable<T>(this IDataResults results, ConstructorInfo preferredConstructor)
        {
            return GetNextEnumerable<T>(results, b => b.UseConstructor(preferredConstructor));
        }

        /// <summary>
        /// Advance to the next result set and map it to an enumerable of objects using the given mapping
        /// compiler and options.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="results"></param>
        /// <param name="setup"></param>
        /// <returns></returns>
        public static IEnumerable<T> GetNextEnumerable<T>(this IDataResults results, Action<IMapCompilerBuilder<T>> setup = null)
        {
            Assert.ArgumentNotNull(results, nameof(results));
            return results.AdvanceToNextResultSet().AsEnumerable<T>(setup);
        }

        public static IEnumerable<T> AsEnumerableAll<T>(this IDataResults results, Action<IMapCompilerBuilder<T>> setup = null)
        {
            return AsEnumerableNextSeveral<T>(results, int.MaxValue, setup);
        }

        public static IEnumerable<T> AsEnumerableNextSeveral<T>(this IDataResults results, int numResultSets, Action<IMapCompilerBuilder<T>> setup = null)
        {
            if (numResultSets <= 0)
                yield break;

            var values = results.AsEnumerable<T>(setup);
            foreach (var value in values)
                yield return value;
            numResultSets--;

            while (numResultSets > 0)
            {
                var hasMore = results.TryAdvanceToNextResultSet();
                if (!hasMore)
                    yield break;
                numResultSets--;
                var nextValues = results.AsEnumerable<T>(setup);
                foreach (var value in nextValues)
                    yield return value;
            }
        }
    }
}