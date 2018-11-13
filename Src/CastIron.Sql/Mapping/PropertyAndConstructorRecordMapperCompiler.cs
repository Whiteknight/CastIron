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
        private static readonly MethodInfo GetValueMethod = typeof(IDataRecord).GetMethod(nameof(IDataRecord.GetValue));
        private static readonly Expression _dbNullExp = Expression.Field(null, typeof(DBNull), nameof(DBNull.Value));

        public Func<IDataRecord, T> CompileExpression<T>(IDataReader reader)
        {
            return CompileExpression<T>(typeof(T), reader, null, null);
        }

        // TODO: Ability to take the IDataRecord in the factory and consume some columns for constructor params so they aren't used later for properties?
        public Func<IDataRecord, T> CompileExpression<T>(Type specific, IDataReader reader, Func<T> factory, ConstructorInfo preferredConstructor)
        {
            Assert.ArgumentNotNull(reader, nameof(reader));
            specific = specific ?? typeof(T);

            var parentType = typeof(T);

            // If parentType is primitive, specific doesn't matter, so we can map a primitive
            if (CompilerTypes.Primitive.Contains(parentType))
                return CompileForPrimitiveType<T>(reader);

            // If asked for an object array, or just an object, return those as object arrays and don't do any fancy mapping
            if (parentType == typeof(object[]) && specific == typeof(object[]))
                return CreateObjectArrayMap(reader) as Func<IDataRecord, T>;
            if (parentType == typeof(object) && specific == typeof(object))
                return CreateObjectArrayMap(reader) as Func<IDataRecord, T>;

            if (parentType.Namespace == "System" && parentType.Name.StartsWith("Tuple") && specific == parentType && factory == null && preferredConstructor == null)
                return CompileTupleMap<T>(reader);

            // Check that specific is an assignable subclass of parentType. At this point we're creating an object
            if (!parentType.IsAssignableFrom(specific))
                throw new Exception($"Type {specific.Name} must be assignable to {typeof(T)}.Name");

            // At this point we're past the easy options and must compile the thunk
            return CompileThunkForObjectInstance(specific, reader, factory, preferredConstructor);
        }

        private static Func<IDataRecord, T> CompileTupleMap<T>(IDataReader reader)
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
                args[i] = GetConversionExpression(i, context, context.Reader.GetFieldType(i), typeParams[i]);

            context.Statements.Add(Expression.Assign(instance, Expression.Call(null, factoryMethod, args)));
            context.Statements.Add(Expression.Convert(instance, typeof(T)));
            var lambdaExpression = Expression.Lambda<Func<IDataRecord, T>>(Expression.Block(context.Parent, context.Variables, context.Statements), context.RecordParam);
            DumpCodeToDebugConsole(lambdaExpression);

            return lambdaExpression.Compile();
        }

        private static Func<IDataRecord, T> CompileThunkForObjectInstance<T>(Type specific, IDataReader reader, Func<T> factory, ConstructorInfo preferredConstructor)
        {
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

        private static ConstructorDetails FindSuitableConstructor(ConstructorInfo preferredConstructor, DataRecordMapperCompileContext context)
        {
            // If the user has specified a preferred constructor, use that.
            if (preferredConstructor != null)
            {
                if (!preferredConstructor.IsPublic || preferredConstructor.IsStatic)
                    throw new Exception("Specified constructor is static or non-public and cannot be used");
                if (preferredConstructor.DeclaringType != context.Specific)
                    throw new Exception($"Specified constructor is for type {preferredConstructor.DeclaringType?.FullName} instead of the expected {context.Specific.FullName}");

                return new ConstructorDetails(preferredConstructor, context.ColumnNames);
            }

            // Otherwise score all constructors and find the best match
            var best = context.Specific.GetConstructors()
                .Select(c => new ConstructorDetails(c, context.ColumnNames))
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

                var conversion = GetConversionExpression(columnIdx, context, context.Reader.GetFieldType(columnIdx), property.PropertyType);
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

        private static NewExpression CreateConstructorCallExpression(ConstructorDetails constructor, DataRecordMapperCompileContext context)
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
                    args[i] = Expression.Constant(GetDefaultValue(parameter.ParameterType));
                    continue;
                }

                // Get the column index and mark the column as being mapped
                context.MappedColumns.Add(name);
                var columnIdx = context.ColumnNames[name];

                args[i] = GetConversionExpression(columnIdx, context, context.Reader.GetFieldType(columnIdx), parameter.ParameterType);
            }

            return Expression.New(constructor.Constructor, args);
        }

        private static Expression GetConversionExpression(int columnIdx, DataRecordMapperCompileContext context, Type columnType, Type targetType)
        {
            // Pull the value out of the reader into the rawVar
            var rawVar = Expression.Variable(typeof(object), "raw_" + columnIdx);
            context.Variables.Add(rawVar);
            var getRawStmt = Expression.Assign(rawVar, Expression.Call(context.RecordParam, GetValueMethod, Expression.Constant(columnIdx)));
            context.Statements.Add(getRawStmt);

            // TODO: Should we store the converted values into variables instead of inlining them?

            // The target is string, regardless of the data type we can .ToString() it
            if (targetType == typeof(string))
            {
                return Expression.Condition(
                    Expression.NotEqual(_dbNullExp, rawVar),
                    Expression.Call(rawVar, nameof(object.ToString), Type.EmptyTypes),
                    Expression.Convert(Expression.Constant(null), typeof(string)));
            }

            // They are the same type, so we can directly assign them
            if (columnType == targetType || (!columnType.IsClass && typeof(Nullable<>).MakeGenericType(columnType) == targetType))
            {
                return Expression.Condition(
                    Expression.NotEqual(_dbNullExp, rawVar),
                    Expression.Convert(
                        rawVar,
                        targetType
                    ),
                    Expression.Convert(Expression.Constant(GetDefaultValue(targetType)), targetType));
            }

            // They are both numeric but not the same type. Unbox and convert
            if (CompilerTypes.Numeric.Contains(columnType) && CompilerTypes.Numeric.Contains(targetType))
            {
                return Expression.Condition(
                    Expression.NotEqual(_dbNullExp, rawVar),
                    Expression.Convert(
                        Expression.Unbox(rawVar, columnType),
                        targetType
                    ),
                    Expression.Convert(Expression.Constant(GetDefaultValue(targetType)), targetType));
            }

            // Target is bool, source type is numeric. Try to coerce by comparing against 0
            if ((targetType == typeof(bool) || targetType == typeof(bool?)) && CompilerTypes.Numeric.Contains(columnType))
            {
                // var result = rawVar is DBNull ? (rawVar != 0) : false
                return Expression.Condition(
                    Expression.NotEqual(_dbNullExp, rawVar),
                    Expression.NotEqual(Expression.Convert(Expression.Constant(0), columnType), Expression.Unbox(rawVar, columnType)),
                    Expression.Constant(false));
            }

            // We fall back to a basic conversion and hope all goes well
            return Expression.Condition(
                Expression.NotEqual(_dbNullExp, rawVar),
                Expression.Convert(
                    rawVar,
                    targetType
                ),
                Expression.Convert(Expression.Constant(GetDefaultValue(targetType)), targetType));
        }

        private static Func<IDataRecord, T> CompileForPrimitiveType<T>(IDataReader reader)
        {
            // TODO: Take an index of the column to use (default=0)
            var recordParam = Expression.Parameter(typeof(IDataRecord), "record");
            var context = new DataRecordMapperCompileContext(reader, recordParam, null, typeof(T), typeof(T));

            var expr = GetConversionExpression(0, context, reader.GetFieldType(0), context.Parent);
            context.Statements.Add(expr);
            var lambdaExpression = Expression.Lambda<Func<IDataRecord, T>>(Expression.Block(context.Parent, context.Variables, context.Statements), recordParam);
            DumpCodeToDebugConsole(lambdaExpression);
            return lambdaExpression.Compile();
        }

        private class ConstructorDetails
        {
            public ConstructorInfo Constructor { get; }

            public ConstructorDetails(ConstructorInfo constructor, IReadOnlyDictionary<string, int> columnNames)
            {
                Constructor = constructor;
                Parameters = constructor.GetParameters();
                Score = ScoreConstructor(columnNames, Parameters);
            }

            public ParameterInfo[] Parameters { get; }

            public int Score { get; }

            private static int ScoreConstructor(IReadOnlyDictionary<string, int> columnNames, ParameterInfo[] parameters)
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

        private static IEnumerable<PropertyInfo> GetMappableProperties(Type specific, DataRecordMapperCompileContext context)
        {
            return specific.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.SetMethod != null && p.GetMethod != null)
                .Where(p => p.CanRead && p.CanWrite)
                .Where(p => !p.GetMethod.IsPrivate && !p.SetMethod.IsPrivate)
                .Where(p => !context.MappedColumns.Contains(p.Name.ToLowerInvariant()));
        }

        private static object GetDefaultValue(Type t)
        {
            return t.IsValueType ? Activator.CreateInstance(t) : null;
        }

        private static Func<IDataRecord, object[]> CreateObjectArrayMap(IDataReader reader)
        {
            var columns = reader.FieldCount;
            return r =>
            {
                var buffer = new object[columns];
                r.GetValues(buffer);
                return buffer;
            };
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