using System;
using System.Collections.Generic;
using System.Data;
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

        /// <summary>
        /// The current index of the result set in the stream. This value is 0 when reading hasn't
        /// started. The first result set will be 1. Streams are forward-only, so this number will
        /// increase but never decrease.
        /// </summary>
        int CurrentSet { get; }

        /// <summary>
        /// Map the result set to objects. This method consumes the reader, making it unavailable
        /// to other methods such as AsRawReader.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="setup">Setup the mapper or mapper compiler using a fluent interface</param>
        /// <returns></returns>
        IEnumerable<T> AsEnumerable<T>(Action<IMapCompilerSettings> setup = null);

        /// <summary>
        /// Advance to the result set in the reader by number, starting with the first result set as 1, 
        /// second result set as 2, etc. Result sets may only be accessed in order, though they can be
        /// skipped. Throws an exception if the request cannot be completed. The exception may contain
        /// information to help understand the nature of the issue.
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        IDataResultsBase AdvanceToResultSet(int num);

        /// <summary>
        /// Try to advance to the result set in the reader by number, starting with the first result set as
        /// 1, the second result set as 2, etc. Result sets may only be accessed in order, though they can
        /// be skipped. Returns false if there is an error, though the exact nature of the error and
        /// steps to remediate will not be communicated (use AdvanceToResultSet) instead.
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        bool TryAdvanceToResultSet(int num);

        /// <summary>
        /// Retrieve an object representing output parameters. Output parameters require the reader
        /// to be closed, so parameters should be read after all result sets are read and mapped.
        /// When this method is called, other methods such as AsEnumerable and AsRawReader may not
        /// be called.
        /// </summary>
        /// <returns></returns>
        ParameterCache GetParameters();

        /// <summary>
        /// Uses the cache to lookup compiled mappings and to save new mappings once they are
        /// compiled. The default cache key used will be the ISqlQuery/ISqlCommand object instance,
        /// so that object should make sure not to change the structure of the result set or the
        /// compilation settings after the first call to .Read(). Notice that the cache holds a 
        /// reference to the object used as the cache key, which will prevent that object from 
        /// being garbage-collected. If you are using cache, make sure you try to reuse your query
        /// object instances
        /// </summary>
        /// <param name="useCache"></param>
        /// <param name="key"></param>
        void CacheMappings(bool useCache, object key = null);
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

#if NETSTANDARD2_1
        IAsyncEnumerable<T> AsEnumerableAsync<T>(Action<IMapCompilerSettings> setup = null);
#endif
    }

    /// <summary>
    /// Common extension methods for IDataResults
    /// </summary>
    public static class DataResultsExtensions
    {
        /// <summary>
        /// Advance to the next result set and map it to an enumerable of objects using the given mapping
        /// compiler and options.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="results"></param>
        /// <param name="setup"></param>
        /// <returns></returns>
        public static IEnumerable<T> GetNextEnumerable<T>(this IDataResultsBase results, Action<IMapCompilerSettings> setup = null)
        {
            Argument.NotNull(results, nameof(results));
            return results.AdvanceToNextResultSet().AsEnumerable<T>(setup);
        }

        /// <summary>
        /// Iterate through all remaining rows in all remaining result sets and map them all to
        /// objects of the same type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="results"></param>
        /// <param name="setup"></param>
        /// <returns></returns>
        public static IEnumerable<T> AsEnumerableAll<T>(this IDataResultsBase results, Action<IMapCompilerSettings> setup = null)
        {
            return AsEnumerableNextSeveral<T>(results, int.MaxValue, setup);
        }

        /// <summary>
        /// Iterate through all remaining rows in the next N result sets, mapping all rows to objects
        /// of the given type. Useful only if the next N result sets all represent the same type 
        /// of object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="results"></param>
        /// <param name="numResultSets">The number of result sets to consume</param>
        /// <param name="setup"></param>
        /// <returns></returns>
        public static IEnumerable<T> AsEnumerableNextSeveral<T>(this IDataResultsBase results, int numResultSets, Action<IMapCompilerSettings> setup = null)
        {
            // TODO: We need to mark the IDataResultsBase object as "locked" so we can't start another read operation or move-next operation while this is running
            Argument.NotNull(results, nameof(results));
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

        /// <summary>
        /// Advance to the next result set, throwing an exception if the request fails
        /// </summary>
        /// <param name="results"></param>
        /// <returns></returns>
        public static IDataResultsBase AdvanceToNextResultSet(this IDataResultsBase results)
        {
            Argument.NotNull(results, nameof(results));
            return results.AdvanceToResultSet(results.CurrentSet + 1);
        }

        /// <summary>
        /// Try to advance to the next result set, returning false if the exception fails
        /// </summary>
        /// <param name="results"></param>
        /// <returns></returns>
        public static bool TryAdvanceToNextResultSet(this IDataResultsBase results)
        {
            Argument.NotNull(results, nameof(results));
            return results.TryAdvanceToResultSet(results.CurrentSet + 1);
        }

        /// <summary>
        /// Return the raw result reader, wrapped to provide better error messages. Error messages
        /// come with some performance penalty, so consider using this for situations where
        /// performance is not a critical concern.
        /// </summary>
        /// <param name="results"></param>
        /// <returns></returns>
        public static IDataReader AsRawReaderWithBetterErrorMessages(this IDataResults results)
        {
            Argument.NotNull(results, nameof(results));
            var reader = results.AsRawReader();
            return new DataReaderWithBetterErrorMessages(reader);
        }

        /// <summary>
        /// Return the raw result reader, wrapped to provide better error messages. Error messages
        /// come with some performance penalty, so consider using this for situations where
        /// performance is not a critical concern.
        /// </summary>
        /// <param name="results"></param>
        /// <returns></returns>
        public static IDataReader AsRawReaderWithBetterErrorMessages(this IDataResultsStream results)
        {
            Argument.NotNull(results, nameof(results));
            var reader = results.AsRawReader();
            return new DataReaderWithBetterErrorMessages(reader);
        }

        /// <summary>
        /// Get the value of the output parameter with the given name
        /// </summary>
        /// <param name="results"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static object GetOutputParameterValue(this IDataResultsBase results, string name)
        {
            Argument.NotNull(results, nameof(results));
            return results.GetParameters().GetValue(name);
        }

        /// <summary>
        /// Get the value of the output parameter with the given name, attempting to convert it
        /// to the specified type if possible. If conversion is not possible, a default value
        /// will be returned instead.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="results"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static T GetOutputParameter<T>(this IDataResultsBase results, string name)
        {
            Argument.NotNull(results, nameof(results));
            return results.GetParameters().GetValue<T>(name);
        }

        /// <summary>
        /// Get the value of the output parameter with the given name, attempting to convert it
        /// to the specified type if possible. If conversion is not possible, an exception will
        /// be thrown.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="results"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static T GetOutputParameterOrThrow<T>(this IDataResultsBase results, string name)
        {
            Argument.NotNull(results, nameof(results));
            return results.GetParameters().GetOutputParameterOrThrow<T>(name);
        }

        /// <summary>
        /// Get all output parameters as a single object, with each parameter being mapped to a
        /// property with the same name (case invariant). Parameters will be mapped to property
        /// types where possible. Otherwise default values will be assigned.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="results"></param>
        /// <returns></returns>
        public static T GetOutputParameters<T>(this IDataResultsBase results)
            where T : class, new()
        {
            Argument.NotNull(results, nameof(results));
            return results.GetParameters().GetOutputParameters<T>();
        }
    }
}