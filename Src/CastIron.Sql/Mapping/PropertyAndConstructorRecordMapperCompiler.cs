using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CastIron.Sql.Utility;

namespace CastIron.Sql.Mapping
{
    public class PropertyAndConstructorRecordMapperCompiler : IRecordMapperCompiler
    {
        private readonly ConstructorFinder _constructorFinder;

        public PropertyAndConstructorRecordMapperCompiler()
        {
            _constructorFinder = new ConstructorFinder();
        }

        // TODO: Ability to take the IDataRecord in the factory and consume some columns for constructor params so they aren't used later for properties?
        public Func<IDataRecord, T> CompileExpression<T>(Type specific, IDataReader reader, Func<T> factory, ConstructorInfo preferredConstructor)
        {
            Assert.ArgumentNotNull(reader, nameof(reader));
            specific = specific ?? typeof(T);
            if (factory != null && preferredConstructor != null)
                throw new Exception($"May specify at most one of {nameof(factory)} or {nameof(preferredConstructor)}");

            // Prepare the list of expressions
            var context = CreateCompileContextForObjectInstance<T>(specific, reader);
            WriteInstantiationExpressionForObjectInstance(factory, preferredConstructor, context);
            WritePropertyAssignmentExpressions(context);

            // Add return statement to return the converted instance
            context.Statements.Add(Expression.Convert(context.Instance, typeof(T)));

            // Compile the expression to a Func<>
            var lambdaExpression = Expression.Lambda<Func<IDataRecord, T>>(Expression.Block(context.Parent, context.Variables, context.Statements), context.RecordParam);
            DumpCodeToDebugConsole(lambdaExpression);
            return lambdaExpression.Compile();
        }

        private void WriteInstantiationExpressionForObjectInstance<T>(Func<T> factory, ConstructorInfo preferredConstructor, DataRecordMapperCompileContext context)
        {
            if (factory != null)
            {
                context.Statements.Add(Expression.Assign(context.Instance, Expression.Call(Expression.Constant(factory.Target), factory.Method)));
                return;
            }

            var constructor = _constructorFinder.FindBestMatch(preferredConstructor, context.Specific, context.ColumnNames);
            var constructorCall = CreateConstructorCallExpression(constructor, context);
            context.Statements.Add(Expression.Assign(context.Instance, constructorCall));
        }

        private static DataRecordMapperCompileContext CreateCompileContextForObjectInstance<T>(Type specific, IDataReader reader)
        {
            var recordParam = Expression.Parameter(typeof(IDataRecord), "record");
            var instance = Expression.Variable(specific, "instance");
            var context = new DataRecordMapperCompileContext(reader, recordParam, instance, typeof(T), specific);
            context.PopulateColumnLookups(reader);
            return context;
        }

        private static void WritePropertyAssignmentExpressions(DataRecordMapperCompileContext context)
        {
            var properties = GetMappableProperties(context.Specific, context);
            foreach (var property in properties)
            {
                var columnIdx = GetColumnIndexForProperty(context, property);
                if (columnIdx < 0)
                    continue;

                var conversion = DataRecordExpressions.GetConversionExpression(columnIdx, context, context.Reader.GetFieldType(columnIdx), property.PropertyType);
                context.Statements.Add(Expression.Call(context.Instance, property.GetSetMethod(), conversion));
            }
        }

        private static int GetColumnIndexForProperty(DataRecordMapperCompileContext context, PropertyInfo property)
        {
            var columnName = property.Name.ToLowerInvariant();
            if (context.ColumnNames.ContainsKey(columnName))
                return context.ColumnNames[columnName];

            columnName = property
                .GetCustomAttributes(typeof(ColumnAttribute), true)
                .Cast<ColumnAttribute>()
                .Select(c => c.Name.ToLowerInvariant())
                .FirstOrDefault();
            if (!string.IsNullOrEmpty(columnName) && context.ColumnNames.ContainsKey(columnName))
                return context.ColumnNames[columnName];

            // TODO: Allow fuzzy match (levenshtein?) or contains match. Configurable, with tolerances.
            return -1;
        }

        private static NewExpression CreateConstructorCallExpression(ConstructorInfo constructor, DataRecordMapperCompileContext context)
        {
            var parameters = constructor.GetParameters();
            // Default parameterless constructor, just call it
            if (parameters.Length == 0)
                return Expression.New(constructor);

            // Map columns to constructor parameters by name, marking the columns consumed so they aren't used again later
            var args = new Expression[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var name = parameter.Name.ToLowerInvariant();
                if (!context.ColumnNames.ContainsKey(name))
                {
                    // The user has specified a constructor where a parameter does not correspond to a column 
                    // Fill in the default value and hope the user knows what they are doing.
                    args[i] = Expression.Constant(DataRecordExpressions.GetDefaultValue(parameter.ParameterType));
                    continue;
                }

                // Get the column index and mark the column as being mapped
                context.MappedColumns.Add(name);
                var columnIdx = context.ColumnNames[name];

                args[i] = DataRecordExpressions.GetConversionExpression(columnIdx, context, context.Reader.GetFieldType(columnIdx), parameter.ParameterType);
            }

            return Expression.New(constructor, args);
        }

        private static IEnumerable<PropertyInfo> GetMappableProperties(Type specific, DataRecordMapperCompileContext context)
        {
            return specific.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.SetMethod != null && p.GetMethod != null)
                .Where(p => p.CanRead && p.CanWrite)
                .Where(p => !p.GetMethod.IsPrivate && !p.SetMethod.IsPrivate)
                .Where(p => !context.MappedColumns.Contains(p.Name.ToLowerInvariant()));
        }

        // Helper method to get a reasonably complete code listing, to help with debugging
        [Conditional("DEBUG")]
        private static void DumpCodeToDebugConsole(Expression expr)
        {
            // This property is marked private, so we can only get to it by reflection, which would be terrible if
            // this wasn't a debug-only helper routine
            var debugViewProp = typeof(Expression).GetProperty("DebugView", BindingFlags.Instance | BindingFlags.NonPublic);
            if (debugViewProp == null)
                return;
            var code = (string) debugViewProp.GetGetMethod(true).Invoke(expr, null);
            Debug.WriteLine(code);
        }
    }
}