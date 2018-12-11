using System;
using System.Data;
using System.Reflection;
using CastIron.Sql.Utility;

namespace CastIron.Sql.Mapping
{
    public interface IRecordMapperCompiler
    {
        /// <summary>
        /// Create a new Lambda function to convert from an IDataRecord to a given type T, using the default compiler.
        /// </summary>
        /// <typeparam name="T">The type of the result record</typeparam>
        /// <param name="specific">The specific type of the record, which must be typeof(T) or an assignable subclass. If null, will default to typeof(T)</param>
        /// <param name="reader">The IDataReader to use to create the mapping.</param>
        /// <param name="factory">If provided, a factory method to create the record instance. Must be null if preferredConstructor is provided</param>
        /// <param name="preferredConstructor">If provided, a preferred constructor to use to create the record instance. Must be null if factory is provided</param>
        /// <returns></returns>
        Func<IDataRecord, T> CompileExpression<T>(Type specific, IDataReader reader, Func<T> factory, ConstructorInfo preferredConstructor);
    }

    public static class RecordMapperCompilerExtensions
    {
        /// <summary>
        /// Create a new Lambda function to convert from an IDataRecord to a given type T.
        /// </summary>
        /// <typeparam name="T">The type of the result record</typeparam>
        /// <param name="compiler">The compiler to use</param>
        /// <param name="reader">The IDataReader to use to create the mapping</param>
        /// <returns></returns>
        public static Func<IDataRecord, T> CompileExpression<T>(this IRecordMapperCompiler compiler, IDataReader reader)
        {
            Assert.ArgumentNotNull(compiler, nameof(compiler));
            return compiler.CompileExpression<T>(typeof(T), reader, null, null);
        }
    }
}