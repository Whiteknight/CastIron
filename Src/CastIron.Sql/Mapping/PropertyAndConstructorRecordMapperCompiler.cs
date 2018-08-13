using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping
{
    public class PropertyAndConstructorRecordMapperCompiler : IRecordMapperCompiler
    {
        private static readonly MethodInfo GetValueMethod = typeof(IDataRecord).GetMethod("GetValue");
        private static readonly Expression _dbNullExp = Expression.Field(null, typeof(DBNull), "Value");

        private static readonly HashSet<Type> _numericTypes = new HashSet<Type>
        {
            typeof(byte),
            typeof(byte?),
            typeof(short),
            typeof(short?),
            typeof(int),
            typeof(int?),
            typeof(long),
            typeof(long?),
            typeof(float),
            typeof(float?),
            typeof(double),
            typeof(double?),
        };

        private static readonly HashSet<Type> _mappableTypes = new HashSet<Type>
        {
            // TODO: Expand this list of all primitive types we can map from the DB
            typeof(byte),
            typeof(byte?),
            typeof(byte[]),
            typeof(short),
            typeof(short?),
            typeof(int),
            typeof(int?),
            typeof(long),
            typeof(long?),
            typeof(float),
            typeof(float?),
            typeof(double),
            typeof(double?),
            typeof(bool),
            typeof(bool?),
            typeof(string),
            typeof(DateTime),
            typeof(DateTime?)
        };

        private static IReadOnlyDictionary<string, int> GetColumnNameMap(IDataReader reader)
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

        public Func<IDataRecord, T> CompileExpression<T>(IDataReader reader)
        {
            if (reader == null)
                return r => default(T);

            var type = typeof(T);
            if (type == typeof(object[]))
                return CreateObjectArrayMap(reader) as Func<IDataRecord, T>;
            if (type == typeof(object))
                return CreateObjectArrayMap(reader) as Func<IDataRecord, T>;

            var recordParam = Expression.Parameter(typeof(IDataRecord), "reader");
            var columnNameMap = GetColumnNameMap(reader);
            var context = new DataRecordMapperCompileContext(columnNameMap, recordParam);

            if (IsPrimitiveType(type))
                return CompileForPrimitiveType<T>(context, recordParam, reader);

            var best = typeof(T).GetConstructors()
                .Select(c => new ConstructorDetails(c, context))
                .Where(x => x.Score >= 0)
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();
            if (best == null)
                throw new Exception($"Cannot find a suitable constructor for type {typeof(T).FullName}");

            NewExpression newExpr = null;
            if (best.Parameters.Length != 0)
            {

                // var rawVar = r.GetValue(columnId);
                // var convertedVar = rawVar == DbNull.Value ? default(targetType) : Convert(rawVar -> targetType);

                var args = new Expression[best.Parameters.Length];
                for (int i = 0; i < best.Parameters.Length; i++)
                {
                    var parameter = best.Parameters[i];
                    var name = parameter.Name.ToLowerInvariant();
                    context.MappedColumns.Add(name);
                    var columnIdx = context.ColumnNames[name];

                    args[i] = GetConversionExpression(columnIdx, context, reader.GetFieldType(columnIdx), parameter.ParameterType);
                }

                newExpr = Expression.New(best.Constructor, args);
            }
            else
                newExpr = Expression.New(best.Constructor);

            var bindingExpressions = new List<MemberAssignment>();
            var properties = GetMappableProperties<T>(context);
            foreach (var property in properties)
            {
                var name = property.Name.ToLowerInvariant();
                if (!context.ColumnNames.ContainsKey(name))
                    continue;
                var columnIdx = context.ColumnNames[name];

                bindingExpressions.Add(Expression.Bind(
                    property,
                    GetConversionExpression(columnIdx, context, reader.GetFieldType(columnIdx), property.PropertyType)
                ));
            }

            context.Statements.Add(Expression.MemberInit(
                newExpr,
                bindingExpressions
            ));

            var lambdaExpression = Expression.Lambda<Func<IDataRecord, T>>(Expression.Block(typeof(T), context.Variables, context.Statements), recordParam);

            //var code = ToCode(lambdaExpression);

            return lambdaExpression.Compile();
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
                    Expression.Call(rawVar, "ToString", Type.EmptyTypes),
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
            if (_numericTypes.Contains(columnType) && _numericTypes.Contains(targetType))
            {
                return Expression.Condition(
                    Expression.NotEqual(_dbNullExp, rawVar),
                    Expression.Convert(
                        Expression.Unbox(rawVar, columnType),
                        targetType
                    ),
                    Expression.Constant(GetDefaultValue(targetType)));
            }

            // Target is bool, but we know source type is numeric. Try to coerce
            if ((targetType == typeof(bool) || targetType == typeof(bool?)) && _numericTypes.Contains(columnType))
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
                Expression.Constant(GetDefaultValue(targetType)));
        }

        private static Func<IDataRecord, T> CompileForPrimitiveType<T>(DataRecordMapperCompileContext context, ParameterExpression recordParam, IDataReader reader)
        {
            var expr = GetConversionExpression(0, context, reader.GetFieldType(0), typeof(T));
            context.Statements.Add(expr);
            var lambdaExpression = Expression.Lambda<Func<IDataRecord, T>>(Expression.Block(typeof(T), context.Variables, context.Statements), recordParam);
            //var code = ToCode(lambdaExpression);
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

                    // TODO: We should be able to get the DataType from the SqlDataReader for the column, and 
                    // add Score+=X if the type is the same and Score+=Y (where Y < X) if they are different but compatible
                    score++;
                }

                return score;
            }
        }

        private static PropertyInfo[] GetMappableProperties<T>(DataRecordMapperCompileContext context)
        {
            return typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
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

        // Helper method to get a reasonably complete code listing, to help with debugging
        private static string ToCode(Expression expr)
        {
            var debugViewProp = typeof(Expression).GetProperty("DebugView", BindingFlags.Instance | BindingFlags.NonPublic);
            return (string)debugViewProp.GetGetMethod(true).Invoke(expr, null);
        }
    }
}