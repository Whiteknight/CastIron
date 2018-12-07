using System;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping
{
    public class PrimitiveRecordMapperCompiler : IRecordMapperCompiler
    {
        private readonly int _columnIndex;

        public PrimitiveRecordMapperCompiler(int columnIndex = 0)
        {
            _columnIndex = columnIndex;
        }

        public Func<IDataRecord, T> CompileExpression<T>(Type specific, IDataReader reader, Func<T> factory, ConstructorInfo preferredConstructor)
        {
            if (!DataRecordExpressions.IsSupportedPrimitiveType(typeof(T)))
                return r => default(T);
            if (!DataRecordExpressions.IsSupportedPrimitiveType(specific))
                return r => default(T);

            var recordParam = Expression.Parameter(typeof(IDataRecord), "record");
            var context = new DataRecordMapperCompileContext(reader, recordParam, null, typeof(T), typeof(T));

            var expr = DataRecordExpressions.GetConversionExpression(_columnIndex, context, context.Parent);
            context.AddStatement(expr);
            return context.CompileLambda<T>();
        }
    }
}