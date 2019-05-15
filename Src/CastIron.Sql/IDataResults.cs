using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using CastIron.Sql.Debugging;
using CastIron.Sql.Mapping;
using CastIron.Sql.Utility;

namespace CastIron.Sql
{
    public interface IDataResultsBase
    {
        /// <summary>
        /// Where available, the number of rows affected by the query. Notice that this number may
        /// not be accurate until all statements in the query have executed and all result sets
        /// are read.
        /// </summary>
        int RowsAffected { get; }

        int CurrentSet { get; }

        /// <summary>
        /// Map the result set to objects
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="setup">Setup the mapper or mapper compiler using a fluent interface</param>
        /// <returns></returns>
        IEnumerable<T> AsEnumerable<T>(Action<IMapCompilerBuilder<T>> setup = null);

        /// <summary>
        /// Advance to the result set in the reader by number, starting with the first result set as 1, 
        /// second result set as 2, etc. Result sets may only be accessed in order, though they can be
        /// skipped. Throws an exception if the specified result set number is lower than the current result
        /// set number, or if the result set with the given number does not exist.
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        IDataResultsBase AdvanceToResultSet(int num);

        /// <summary>
        /// Try to advance to the result set in the reader by number, starting with the first result set as
        /// 1, the second result set as 2, etc. Result sets may only be accessed in order, though they can
        /// be skipped. Returns false if attempting to access a previous result set or if the specified
        /// result set does not exist.
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        bool TryAdvanceToResultSet(int num);

        ParameterCache GetParameters();
    }

    /// <summary>
    /// Combined results from the completed database interaction. Acts as a wrapper around IDataReader and 
    /// the output parameters (if any) from the IDbCommand.
    /// </summary>
    public interface IDataResults : IDataResultsBase
    {
        /// <summary>
        /// Get a reference to the raw IDataReader. WARNING: When you access the underlying reader
        /// object you assume complete control over it and this object will no longer be functional.
        /// You cannot call any other methods on IDataResults but instead may only call methods on
        /// the reader. 
        /// </summary>
        /// <returns></returns>
        IDataReader AsRawReader();

    }

    public interface IDataResultsStream : IDataResultsBase, IDisposable
    {
        /// <summary>
        /// Get a reference to the raw IDataReader. WARNING: When you access the underlying reader
        /// object you assume complete control over it and this object will no longer be functional.
        /// You cannot call any other methods on IDataResults and you MUST call .Dispose() on the
        /// reader yourself to ensure resources are cleaned up.
        /// </summary>
        /// <returns></returns>
        IDataReader AsRawReader();
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
        public static IEnumerable<T> AsEnumerable<T>(this IDataResultsBase results, Func<T> factory)
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
        public static IEnumerable<T> AsEnumerable<T>(this IDataResultsBase results, ConstructorInfo preferredConstructor)
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
        public static IEnumerable<T> GetNextEnumerable<T>(this IDataResultsBase results, Func<T> factory)
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
        public static IEnumerable<T> GetNextEnumerable<T>(this IDataResultsBase results, ConstructorInfo preferredConstructor)
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
        public static IEnumerable<T> GetNextEnumerable<T>(this IDataResultsBase results, Action<IMapCompilerBuilder<T>> setup = null)
        {
            Assert.ArgumentNotNull(results, nameof(results));
            return results.AdvanceToNextResultSet().AsEnumerable<T>(setup);
        }

        public static IEnumerable<T> AsEnumerableAll<T>(this IDataResultsBase results, Action<IMapCompilerBuilder<T>> setup = null)
        {
            return AsEnumerableNextSeveral<T>(results, int.MaxValue, setup);
        }

        public static IEnumerable<T> AsEnumerableNextSeveral<T>(this IDataResultsBase results, int numResultSets, Action<IMapCompilerBuilder<T>> setup = null)
        {
            Assert.ArgumentNotNull(results, nameof(results));
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

        public static IDataResultsBase AdvanceToNextResultSet(this IDataResultsBase results)
        {
            Assert.ArgumentNotNull(results, nameof(results));
            return results.AdvanceToResultSet(results.CurrentSet + 1);
        }

        public static bool TryAdvanceToNextResultSet(this IDataResultsBase results)
        {
            Assert.ArgumentNotNull(results, nameof(results));
            return results.TryAdvanceToResultSet(results.CurrentSet + 1);
        }

        public static IDataReader AsRawReaderWithBetterErrorMessages(this IDataResults results)
        {
            Assert.ArgumentNotNull(results, nameof(results));
            var reader = results.AsRawReader();
            return new DataReaderWithBetterErrorMessages(reader);
        }

        public static IDataReader AsRawReaderWithBetterErrorMessages(this IDataResultsStream results)
        {
            Assert.ArgumentNotNull(results, nameof(results));
            var reader = results.AsRawReader();
            return new DataReaderWithBetterErrorMessages(reader);
        }

        public static object GetOutputParameterValue(this IDataResultsBase results, string name)
        {
            Assert.ArgumentNotNull(results, nameof(results));
            return results.GetParameters().GetValue(name);
        }

        public static T GetOutputParameter<T>(this IDataResultsBase results, string name)
        {
            Assert.ArgumentNotNull(results, nameof(results));
            return results.GetParameters().GetValue<T>(name);
        }

        public static T GetOutputParameterOrThrow<T>(this IDataResultsBase results, string name)
        {
            Assert.ArgumentNotNull(results, nameof(results));
            return results.GetParameters().GetOutputParameterOrThrow<T>(name);
        }

        public static T GetOutputParameters<T>(this IDataResultsBase results)
            where T : class, new()
        {
            Assert.ArgumentNotNull(results, nameof(results));
            return results.GetParameters().GetOutputParameters<T>();
        }
    }
}