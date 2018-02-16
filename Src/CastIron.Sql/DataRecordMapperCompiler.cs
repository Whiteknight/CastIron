﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql
{
    public class DataRecordMapperCompileContext
    {
        public DataRecordMapperCompileContext(IReadOnlyDictionary<string, int> columnNames, ParameterExpression recordParam)
        {
            ColumnNames = columnNames;
            RecordParam = recordParam;
            BindingExpressions = new List<MemberAssignment>();
            MappedColumns = new HashSet<string>();
        }

        public IReadOnlyDictionary<string, int> ColumnNames { get;  }
        public ParameterExpression RecordParam { get; }

        public List<MemberAssignment> BindingExpressions { get; }

        public HashSet<string> MappedColumns { get; }
    }

    public static class DataRecordMapperCompiler
    {
        private static readonly MethodInfo GetValueMethod = typeof(IDataRecord).GetMethod("GetValue");
        private static readonly Expression _dbNullExp = Expression.Field(null, typeof(DBNull), "Value");
        private static readonly HashSet<Type> _mappableTypes = new HashSet<Type>
        {
            typeof(int),
            typeof(int?),
            typeof(long),
            typeof(long?),
            typeof(bool),
            typeof(bool?),
            typeof(string),
            typeof(DateTime),
            typeof(DateTime?)
        };

        public static Func<IDataRecord, T> CompileExpression<T>(IReadOnlyList<string> columnNames)
        {
            var dict = new Dictionary<string, int>();
            for (int i = 0; i < columnNames.Count; i++)
            {
                if (string.IsNullOrEmpty(columnNames[i]))
                    continue;
                dict.Add(columnNames[i], i);
            }

            return CompileExpression<T>(dict);
        }

        public static Func<IDataRecord, T> CompileExpression<T>(IReadOnlyDictionary<string, int> columnNames)
        {
            var type = typeof(T);
            if (IsPrimitiveType(type))
                return CompileForPrimitiveType<T>();

            var recordParam = Expression.Parameter(typeof(IDataRecord), "reader");
            var context = new DataRecordMapperCompileContext(columnNames, recordParam);

            var newExpr = GetConstructorExpression<T>(context);

            var initExpr = Expression.MemberInit(
                newExpr,
                GetBindingExpressions<T>(context)
            );

            return Expression.Lambda<Func<IDataRecord, T>>(initExpr, recordParam).Compile();
        }

        private static Func<IDataRecord, T> CompileForPrimitiveType<T>()
        {
            return r =>
            {
                var value = r.GetValue(0);
                if (value == DBNull.Value)
                    return default(T);
                return (T) value;
            };
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

        private static NewExpression GetConstructorExpression<T>(DataRecordMapperCompileContext context)
        {
            var best = typeof(T).GetConstructors()
                .Select(c => new ConstructorDetails(c, context))
                .Where(x => x.Score >= 0)
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();
            if (best == null)
                throw new Exception($"Cannot find a suitable constructor for type {typeof(T).FullName}");
            if (best.Parameters.Length == 0)
                return Expression.New(best.Constructor);

            var args = new Expression[best.Parameters.Length];
            for (int i = 0; i < best.Parameters.Length; i++)
            {
                var parameter = best.Parameters[i];
                var name = parameter.Name.ToLowerInvariant();
                context.MappedColumns.Add(name);
                var columnIdx = context.ColumnNames[name];

                args[i] = CreateValueFetchExpression(context, columnIdx, parameter.ParameterType);
            }
            
            return Expression.New(best.Constructor, args);
        }

        private static IEnumerable<MemberAssignment> GetBindingExpressions<T>(DataRecordMapperCompileContext context)
        {
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
                    CreateValueFetchExpression(context, columnIdx, property.PropertyType)
                ));
            }

            return bindingExpressions;
        }

        private static ConditionalExpression CreateValueFetchExpression(DataRecordMapperCompileContext context, int columnIdx, Type targetType)
        {
            var columnValueExpression = Expression.Call(context.RecordParam, GetValueMethod, Expression.Constant(columnIdx));
            return Expression.Condition(
                Expression.NotEqual(_dbNullExp, columnValueExpression),
                Expression.Convert(
                    columnValueExpression,
                    targetType
                ),
                Expression.Convert(
                    Expression.Constant(GetDefaultValue(targetType)),
                    targetType
                )
            );
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
    }
}