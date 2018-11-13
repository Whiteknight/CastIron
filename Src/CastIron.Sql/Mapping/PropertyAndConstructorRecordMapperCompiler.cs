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
        // TODO: Ability to take the IDataRecord in the factory and consume some columns for constructor params so they aren't used later for properties?
        public Func<IDataRecord, T> CompileExpression<T>(Type specific, IDataReader reader, Func<T> factory, ConstructorInfo preferredConstructor)
        {
            Assert.ArgumentNotNull(reader, nameof(reader));
            specific = specific ?? typeof(T);

            var context = CreateCompileContextForObjectInstance<T>(specific, reader);
            WriteInstantiationExpressionForObjectInstance(factory, preferredConstructor, context);
            WritePropertyAssignmentExpressions(context);
            // Add return statement to return the converted instance
            context.Statements.Add(Expression.Convert(context.Instance, typeof(T)));
            var lambdaExpression = Expression.Lambda<Func<IDataRecord, T>>(Expression.Block(context.Parent, context.Variables, context.Statements), context.RecordParam);
            DumpCodeToDebugConsole(lambdaExpression);

            return lambdaExpression.Compile();
        }

        private static void WriteInstantiationExpressionForObjectInstance<T>(Func<T> factory, ConstructorInfo preferredConstructor, DataRecordMapperCompileContext context)
        {
            if (factory != null)
            {
                if (preferredConstructor != null)
                    throw new Exception($"May specify at most one of {nameof(factory)} or {nameof(preferredConstructor)}");

                context.Statements.Add(Expression.Assign(context.Instance, Expression.Call(Expression.Constant(factory.Target), factory.Method)));
                return;
            }

            var constructor = FindSuitableConstructor(preferredConstructor, context);
            var constructorCall = CreateConstructorCallExpression(constructor, context);
            context.Statements.Add(Expression.Assign(context.Instance, constructorCall));
        }

        private static ScoredConstructor FindSuitableConstructor(ConstructorInfo preferredConstructor, DataRecordMapperCompileContext context)
        {
            // If the user has specified a preferred constructor, use that.
            if (preferredConstructor != null)
            {
                if (!preferredConstructor.IsPublic || preferredConstructor.IsStatic)
                    throw new Exception("Specified constructor is static or non-public and cannot be used");
                if (preferredConstructor.DeclaringType != context.Specific)
                    throw new Exception($"Specified constructor is for type {preferredConstructor.DeclaringType?.FullName} instead of the expected {context.Specific.FullName}");

                return new ScoredConstructor(preferredConstructor, context.ColumnNames);
            }

            // Otherwise score all constructors and find the best match
            var best = context.Specific.GetConstructors()
                .Select(c => new ScoredConstructor(c, context.ColumnNames))
                .Where(x => x.Score >= 0)
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();
            if (best == null)
                throw new Exception($"Cannot find a suitable constructor for type {context.Specific.FullName}");
            return best;
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

        private static NewExpression CreateConstructorCallExpression(ScoredConstructor constructor, DataRecordMapperCompileContext context)
        {
            // Default parameterless constructor, just call it
            if (constructor.Parameters.Length == 0)
                return Expression.New(constructor.Constructor);

            // Map columns to constructor parameters by name, marking the columns consumed so they aren't used again later
            var args = new Expression[constructor.Parameters.Length];
            for (var i = 0; i < constructor.Parameters.Length; i++)
            {
                var parameter = constructor.Parameters[i];
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

            return Expression.New(constructor.Constructor, args);
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

    public class ScoredConstructor
    {
        public ConstructorInfo Constructor { get; }

        public ScoredConstructor(ConstructorInfo constructor, IReadOnlyDictionary<string, int> columnNames)
        {
            Constructor = constructor;
            Parameters = constructor.GetParameters();
            Score = ScoreConstructorByParameterNames(columnNames, Parameters);
        }

        // TODO: Also score a constructor by an array of types

        public ParameterInfo[] Parameters { get; }

        public int Score { get; }

        private static int ScoreConstructorByParameterNames(IReadOnlyDictionary<string, int> columnNames, ParameterInfo[] parameters)
        {
            int score = 0;
            foreach (var param in parameters)
            {
                var name = param.Name.ToLowerInvariant();
                if (!columnNames.ContainsKey(name))
                    return -1;
                if (!CompilerTypes.Primitive.Contains(param.ParameterType))
                    return -1;

                // TODO: check that the types are compatible. Add a big delta where the match is easy, smaller delta where the match requires conversion
                // TODO: Return -1 where the types cannot be converted
                score++;
            }

            return score;
        }
    }
}