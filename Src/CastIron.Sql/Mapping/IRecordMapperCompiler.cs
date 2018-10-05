using System;
using System.Data;
using System.Reflection;

namespace CastIron.Sql.Mapping
{
    public interface IRecordMapperCompiler
    {
        Func<IDataRecord, T> CompileExpression<T>(IDataReader reader);
        Func<IDataRecord, T> CompileExpression<T>(Type specific, IDataReader reader, Func<T> factory, ConstructorInfo preferredConstructor);
    }
}