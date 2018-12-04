using System;
using System.Data;
using System.Diagnostics;
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
            if (!CompilerTypes.Primitive.Contains(typeof(T)))
                return r => default(T);
            if (!CompilerTypes.Primitive.Contains(specific))
                return r => default(T);

            var recordParam = Expression.Parameter(typeof(IDataRecord), "record");
            var context = new DataRecordMapperCompileContext(reader, recordParam, null, typeof(T), typeof(T));

            var expr = DataRecordExpressions.GetConversionExpression(_columnIndex, context, reader.GetFieldType(_columnIndex), context.Parent);
            context.Statements.Add(expr);
            var lambdaExpression = Expression.Lambda<Func<IDataRecord, T>>(Expression.Block(context.Parent, context.Variables, context.Statements), recordParam);
            DumpCodeToDebugConsole(lambdaExpression);
            return lambdaExpression.Compile();
        }

        [Conditional("DEBUG")]
        private static void DumpCodeToDebugConsole(Expression expr)
        {
            // This property is marked private, so we can only get to it by reflection, which would be terrible if
            // this wasn't a debug-only helper routine
            var debugViewProp = typeof(Expression).GetProperty("DebugView", BindingFlags.Instance | BindingFlags.NonPublic);
            if (debugViewProp == null)
                return;
            var code = (string)debugViewProp.GetGetMethod(true).Invoke(expr, null);
            Debug.WriteLine(code);
        }
    }
}