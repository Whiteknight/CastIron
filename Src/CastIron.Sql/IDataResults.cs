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
        /// Map the current result set to an enumerable of objects, using the specified mapping callback
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="map"></param>
        /// <returns></returns>
        IEnumerable<T> AsEnumerable<T>(Func<IDataRecord, T> map = null);

        /// <summary>
        /// Map the current result set to an enumerable of objects, using the specified mapping compiler and 
        /// constructor information
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="compiler"></param>
        /// <param name="factory"></param>
        /// <param name="preferredConstructor"></param>
        /// <returns></returns>
        IEnumerable<T> AsEnumerable<T>(IRecordMapperCompiler compiler, Func<T> factory = null, ConstructorInfo preferredConstructor = null);

        /// <summary>
        /// Map the current result set to an enumerable of objects, where different subclasses may be
        /// instantiated depending on the data in the record.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="setup"></param>
        /// <param name="compiler"></param>
        /// <returns></returns>
        IEnumerable<T> AsEnumerable<T>(Func<ISubclassMapping<T>, ISubclassMapping<T>> setup, IRecordMapperCompiler compiler = null);

        /// <summary>
        /// Get the value of the output parameter by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        object GetOutputParameter(string name);

        /// <summary>
        /// Get the value of the output parameter by name, coerced to the specified type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        T GetOutputParameter<T>(string name);

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
            return results.AsEnumerable<T>(null, factory, null);
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
            return results.AsEnumerable<T>(null, null, preferredConstructor);
        }

        /// <summary>
        /// Advance to the next result set and map it to an enumerable of objects using the given mapping
        /// callback.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="results"></param>
        /// <param name="map"></param>
        /// <returns></returns>
        public static IEnumerable<T> GetNextEnumerable<T>(this IDataResults results, Func<IDataRecord, T> map = null)
        {
            Assert.ArgumentNotNull(results, nameof(results));
            return results.AdvanceToNextResultSet().AsEnumerable<T>(map);
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
            return GetNextEnumerable<T>(results, null, factory, null);
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
            return GetNextEnumerable<T>(results, null, null, preferredConstructor);
        }

        /// <summary>
        /// Advance to the next result set and map it to an enumerable of objects using the given mapping
        /// compiler and options.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="results"></param>
        /// <param name="compiler"></param>
        /// <param name="factory"></param>
        /// <param name="preferredConstructor"></param>
        /// <returns></returns>
        public static IEnumerable<T> GetNextEnumerable<T>(this IDataResults results, IRecordMapperCompiler compiler, Func<T> factory = null, ConstructorInfo preferredConstructor = null)
        {
            Assert.ArgumentNotNull(results, nameof(results));
            return results.AdvanceToNextResultSet().AsEnumerable<T>(compiler, factory, preferredConstructor);
        }

        /// <summary>
        /// Advance to the next result set and map it to an enumerable of objects where different subclasses
        /// may be selected depending on the data in the record.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="results"></param>
        /// <param name="setup"></param>
        /// <param name="compiler"></param>
        /// <returns></returns>
        public static IEnumerable<T> GetNextEnumerable<T>(this IDataResults results, Func<ISubclassMapping<T>, ISubclassMapping<T>> setup, IRecordMapperCompiler compiler = null)
        {
            Assert.ArgumentNotNull(results, nameof(results));
            return results.AdvanceToNextResultSet().AsEnumerable<T>(setup, compiler);
        }
    }
}