using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using CastIron.Sql.Mapping;

namespace CastIron.Sql
{
    public interface IDataResults
    {
        IDataReader AsRawReader();
        IDataReader AsRawReaderWithBetterErrorMessages();
        IEnumerable<T> AsEnumerable<T>(Func<IDataRecord, T> map = null);
        IEnumerable<T> AsEnumerable<T>(IRecordMapperCompiler compiler, Func<T> factory = null, ConstructorInfo preferredConstructor = null);
        IEnumerable<T> AsEnumerable<T>(Func<ISubclassMapping<T>, ISubclassMapping<T>> setup, IRecordMapperCompiler compiler = null);
        object GetOutputParameter(string name);
        T GetOutputParameter<T>(string name);

        T GetOutputParameters<T>()
            where T : class, new();

        IDataResults AdvanceToNextResultSet();
        IDataResults AdvanceToResultSet(int num);
    }

    public static class DataResultsExtensions
    {
        public static IEnumerable<T> AsEnumerable<T>(this IDataResults results, Func<T> factory)
        {
            return results.AsEnumerable<T>(null, factory, null);
        }

        public static IEnumerable<T> AsEnumerable<T>(this IDataResults results, ConstructorInfo preferredConstructor)
        {
            return results.AsEnumerable<T>(null, null, preferredConstructor);
        }

        public static IEnumerable<T> GetNextEnumerable<T>(this IDataResults results, Func<IDataRecord, T> map = null)
        {
            return results.AdvanceToNextResultSet().AsEnumerable<T>(map);
        }

        public static IEnumerable<T> GetNextEnumerable<T>(this IDataResults results, Func<T> factory)
        {
            return GetNextEnumerable<T>(results, null, factory, null);
        }

        public static IEnumerable<T> GetNextEnumerable<T>(this IDataResults results, ConstructorInfo preferredConstructor)
        {
            return GetNextEnumerable<T>(results, null, null, preferredConstructor);
        }

        public static IEnumerable<T> GetNextEnumerable<T>(this IDataResults results, IRecordMapperCompiler compiler, Func<T> factory = null, ConstructorInfo preferredConstructor = null)
        {
            return results.AdvanceToNextResultSet().AsEnumerable<T>(compiler, factory, preferredConstructor);
        }

        public static IEnumerable<T> GetNextEnumerable<T>(this IDataResults results, Func<ISubclassMapping<T>, ISubclassMapping<T>> setup, IRecordMapperCompiler compiler = null)
        {
            return results.AdvanceToNextResultSet().AsEnumerable<T>(setup, compiler);
        }
    }
}