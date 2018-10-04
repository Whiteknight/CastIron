using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping
{
    public class PropertyAndConstructorRecordMapperCompiler : IRecordMapperCompiler
    {
        private static readonly MethodInfo GetValueMethod = typeof(IDataRecord).GetMethod(nameof(IDataRecord.GetValue));
        private static readonly Expression _dbNullExp = Expression.Field(null, typeof(DBNull), nameof(DBNull.Value));

        public static readonly HashSet<Type> NumericTypes = new HashSet<Type>
        {
            typeof(byte),
            typeof(byte?),
            typeof(decimal),
            typeof(decimal?),
            typeof(double),
            typeof(double?),
            typeof(float),
            typeof(float?),
            typeof(int),
            typeof(int?),
            typeof(long),
            typeof(long?),
            typeof(short),
            typeof(short?)
        };

        private static readonly HashSet<Type> _mappableTypes = new HashSet<Type>
        {
            // TODO: Are there any other types we can map from the DB?
            typeof(bool),
            typeof(bool?),
            typeof(byte),
            typeof(byte?),
            typeof(byte[]),
            typeof(DateTime),
            typeof(DateTime?),
            typeof(decimal),
            typeof(decimal?),
            typeof(double),
            typeof(double?),
            typeof(float),
            typeof(float?),
            typeof(Guid),
            typeof(Guid?),
            typeof(int),
            typeof(int?),
            typeof(long),
            typeof(long?),
            typeof(short),
            typeof(short?),
            typeof(string)
        };

        public Func<IDataRecord, T> CompileExpression<T>(IDataReader reader)
        {
            return CompileExpression<T>(typeof(T), reader);
        }

        public Func<IDataRecord, T> CompileExpression<T>(Type specific, IDataReader reader)
        {
            if (!typeof(T).IsAssignableFrom(specific))
                throw new Exception($"Type {specific.Name} must be assignable to {typeof(T)}.Name");

            if (reader == null)
                return r => default(T);

            var type = typeof(T);
            if (type == typeof(object[]))
                return CreateObjectArrayMap(reader) as Func<IDataRecord, T>;
            if (type == typeof(object))
                return CreateObjectArrayMap(reader) as Func<IDataRecord, T>;
            if (IsPrimitiveType(type))
                return CompileForPrimitiveType<T>(reader);

            return CreateConstructorAndPropertyMap<T>(specific, reader);
        }

        private static Func<IDataRecord, T> CreateConstructorAndPropertyMap<T>(Type specific, IDataReader reader)
        {
            var recordParam = Expression.Parameter(typeof(IDataRecord), "record");
            var columnNameMap = GetColumnNameLookup(reader);
            var context = new DataRecordMapperCompileContext(columnNameMap, recordParam);

            var constructor = GetBestConstructor(specific, context);

            return CreateNewObjectLambda<T>(specific, reader, constructor, context, recordParam);
        }

        private static Func<IDataRecord, T> CreateNewObjectLambda<T>(Type specific, IDataReader reader, ConstructorDetails constructor, DataRecordMapperCompileContext context, ParameterExpression recordParam)
        {
            var newExpr = CreateConstructorCallExpression(reader, constructor, context);

            var bindingExpressions = GetPropertyBindingExpressions(specific, reader, context);

            context.Statements.Add(Expression.MemberInit(
                newExpr,
                bindingExpressions
            ));

            var lambdaExpression = Expression.Lambda<Func<IDataRecord, T>>(Expression.Block(typeof(T), context.Variables, context.Statements), recordParam);
            DumpCodeToDebugConsole(lambdaExpression);

            return lambdaExpression.Compile();
        }

        private static List<MemberAssignment> GetPropertyBindingExpressions(Type specific, IDataReader reader, DataRecordMapperCompileContext context)
        {
            var bindingExpressions = new List<MemberAssignment>();
            var properties = GetMappableProperties(specific, context);
            foreach (var property in properties)
            {
                // TODO: Support ColumnAttribute.Name here, for alternate possible names
                var name = property.Name.ToLowerInvariant();
                if (!context.ColumnNames.ContainsKey(name))
                    continue;
                var columnIdx = context.ColumnNames[name];

                bindingExpressions.Add(Expression.Bind(
                    property,
                    GetConversionExpression(columnIdx, context, reader.GetFieldType(columnIdx), property.PropertyType)
                ));
            }

            return bindingExpressions;
        }

        private static NewExpression CreateConstructorCallExpression(IDataReader reader, ConstructorDetails constructor, DataRecordMapperCompileContext context)
        {
            if (constructor.Parameters.Length == 0)
                return Expression.New(constructor.Constructor);

            var args = new Expression[constructor.Parameters.Length];
            for (int i = 0; i < constructor.Parameters.Length; i++)
            {
                var parameter = constructor.Parameters[i];
                var name = parameter.Name.ToLowerInvariant();
                context.MappedColumns.Add(name);
                var columnIdx = context.ColumnNames[name];

                args[i] = GetConversionExpression(columnIdx, context, reader.GetFieldType(columnIdx), parameter.ParameterType);
            }

            return Expression.New(constructor.Constructor, args);
        }

        private static ConstructorDetails GetBestConstructor(Type specific, DataRecordMapperCompileContext context)
        {
            var best = specific.GetConstructors()
                .Select(c => new ConstructorDetails(c, context))
                .Where(x => x.Score >= 0)
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();
            if (best == null)
                throw new Exception($"Cannot find a suitable constructor for type {specific.FullName}");
            return best;
        }

        private static Expression GetConversionExpression(int columnIdx, DataRecordMapperCompileContext context, Type columnType, Type targetType)
        {
            var rawVar = Expression.Variable(typeof(object), "raw_" + columnIdx);
            context.Variables.Add(rawVar);
            var getRawStmt = Expression.Assign(rawVar, Expression.Call(context.RecordParam, GetValueMethod, Expression.Constant(columnIdx)));
            context.Statements.Add(getRawStmt);

            // The target is string
            if (targetType == typeof(string))
            {
                return Expression.Condition(
                    Expression.NotEqual(_dbNullExp, rawVar),
                    Expression.Call(rawVar, nameof(object.ToString), Type.EmptyTypes),
                    Expression.Convert(Expression.Constant(null), typeof(string)));
            }

            // They are the same type
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

            // They are both numeric but not the same type
            if (NumericTypes.Contains(columnType) && NumericTypes.Contains(targetType))
            {
                return Expression.Condition(
                    Expression.NotEqual(_dbNullExp, rawVar),
                    Expression.Convert(
                        Expression.Unbox(rawVar, columnType),
                        targetType
                    ),
                    Expression.Convert(Expression.Constant(GetDefaultValue(targetType)), targetType));
            }

            // Target is bool, but we know source type is numeric. Try to coerce
            if ((targetType == typeof(bool) || targetType == typeof(bool?)) && NumericTypes.Contains(columnType))
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
            var recordParam = Expression.Parameter(typeof(IDataRecord), "record");
            var context = new DataRecordMapperCompileContext(null, recordParam);

            var expr = GetConversionExpression(0, context, reader.GetFieldType(0), typeof(T));
            context.Statements.Add(expr);
            var lambdaExpression = Expression.Lambda<Func<IDataRecord, T>>(Expression.Block(typeof(T), context.Variables, context.Statements), recordParam);
            DumpCodeToDebugConsole(lambdaExpression);
            return lambdaExpression.Compile();
        }

        private static bool IsPrimitiveType(Type type)
        {
            return _mappableTypes.Contains(type);
        }

        private class ConstructorDetails
        {
            public ConstructorInfo Constructor { get; }

            public ConstructorDetails(ConstructorInfo constructor, DataRecordMapperCompileContext context)
            {
                Constructor = constructor;
                Parameters = constructor.GetParameters();
                Score = ScoreConstructor(context, Parameters);
            }

            public ParameterInfo[] Parameters { get; }

            public int Score { get; }

            private static int ScoreConstructor(DataRecordMapperCompileContext context, ParameterInfo[] parameters)
            {
                int score = 0;
                foreach (var param in parameters)
                {
                    var name = param.Name.ToLowerInvariant();
                    if (!context.ColumnNames.ContainsKey(name))
                        return -1;
                    if (!_mappableTypes.Contains(param.ParameterType))
                        return -1;

                    // TODO: check that the types are compatible. Add a big delta where the match is easy, smaller delta where the match requires conversion
                    // TODO: Return -1 where the types cannot be converted
                    score++;
                }

                return score;
            }
        }

        private static PropertyInfo[] GetMappableProperties(Type specific, DataRecordMapperCompileContext context)
        {
            return specific.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.SetMethod != null && p.GetMethod != null)
                .Where(p => p.CanRead && p.CanWrite)
                .Where(p => !p.GetMethod.IsPrivate && !p.SetMethod.IsPrivate)
                .Where(p => !context.MappedColumns.Contains(p.Name.ToLowerInvariant()))
                .ToArray();
        }

        private static object GetDefaultValue(Type t)
        {
            object defaultValue = null;
            if (t.IsValueType)
                defaultValue = Activator.CreateInstance(t);
            return defaultValue;
        }

        private static IReadOnlyDictionary<string, int> GetColumnNameLookup(IDataReader reader)
        {
            var columnNames = new Dictionary<string, int>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var name = reader.GetName(i).ToLowerInvariant();
                if (string.IsNullOrEmpty(name))
                    continue;
                if (columnNames.ContainsKey(name))
                    continue;
                columnNames.Add(name, i);
            }

            return columnNames;
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