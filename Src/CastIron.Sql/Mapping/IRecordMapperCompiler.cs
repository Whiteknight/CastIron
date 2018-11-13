using System;
using System.Data;
using System.Reflection;

namespace CastIron.Sql.Mapping
{
    public interface IRecordMapperCompiler
    {
        Func<IDataRecord, T> CompileExpression<T>(Type specific, IDataReader reader, Func<T> factory, ConstructorInfo preferredConstructor);
    }

    public static class RecordMapperCompilerExtensions
    {
        public static Func<IDataRecord, T> CompileExpression<T>(this IRecordMapperCompiler compiler, IDataReader reader)
        {
            return compiler.CompileExpression<T>(typeof(T), reader, null, null);
        }
    }
}