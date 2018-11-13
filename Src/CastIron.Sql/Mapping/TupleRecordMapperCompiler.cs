using System;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping
{
    public class TupleRecordMapperCompiler : IRecordMapperCompiler
    {
        public Func<IDataRecord, T> CompileExpression<T>(Type specific, IDataReader reader, Func<T> factory, ConstructorInfo preferredConstructor)
        {
            var tupleType = typeof(T);
            var recordParam = Expression.Parameter(typeof(IDataRecord), "record");
            var instance = Expression.Variable(tupleType, "instance");
            var context = new DataRecordMapperCompileContext(reader, recordParam, instance, typeof(T), tupleType);

            var typeParams = tupleType.GenericTypeArguments;
            if (typeParams.Length == 0 || typeParams.Length > 7)
                throw new Exception($"Cannot create a tuple with {typeParams.Length} parameters. Must be between 1 and 7");
            var factoryMethod = typeof(Tuple).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == nameof(Tuple.Create) && m.GetParameters().Length == typeParams.Length)
                .Select(m => m.MakeGenericMethod(typeParams))
                .FirstOrDefault();
            if (factoryMethod == null)
                throw new Exception($"Cannot find factory method for type {tupleType.Name}");
            var args = new Expression[typeParams.Length];
            for (var i = 0; i < typeParams.Length; i++)
                args[i] = DataRecordExpressions.GetConversionExpression(i, context, context.Reader.GetFieldType(i), typeParams[i]);

            context.Statements.Add(Expression.Assign(instance, Expression.Call(null, factoryMethod, args)));
            context.Statements.Add(Expression.Convert(instance, typeof(T)));
            var lambdaExpression = Expression.Lambda<Func<IDataRecord, T>>(Expression.Block(context.Parent, context.Variables, context.Statements), context.RecordParam);
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