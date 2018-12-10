using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CastIron.Sql.Utility;

namespace CastIron.Sql.Mapping
{
    public class DataRecordMapperCompileContext
    {
        private readonly Dictionary<string, List<ColumnInfo>> _columnNames;
        private readonly List<ParameterExpression> _variables;
        private readonly List<Expression> _statements;
        private int _varNumber;

        private class ColumnInfo
        {
            public int Index { get; }

            public ColumnInfo(int index)
            {
                Index = index;
            }

            public bool Mapped { get; set; }
        }

        public DataRecordMapperCompileContext(IDataReader reader, ParameterExpression recordParam, ParameterExpression instance, Type parent, Type specific)
        {
            Assert.ArgumentNotNull(reader, nameof(reader));
            Assert.ArgumentNotNull(recordParam, nameof(recordParam));

            Reader = reader;
            RecordParam = recordParam;
            Instance = instance;
            Parent = parent;
            Specific = specific ?? parent;

            _variables = new List<ParameterExpression>();
            if (instance != null)
                _variables.Add(instance);
            _statements = new List<Expression>();
            _columnNames = new Dictionary<string, List<ColumnInfo>>();
            _varNumber = 0;
        }
        
        public IDataReader Reader { get; }
        public ParameterExpression RecordParam { get; }
        public ParameterExpression Instance { get; }
        public Type Parent { get; }
        public Type Specific { get; }

        //public List<ParameterExpression> Variables { get; }
        //public List<Expression> Statements { get; }

        public void PopulateColumnLookups(IDataReader reader)
        {
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var name = (reader.GetName(i) ?? "").ToLowerInvariant();
                if (!_columnNames.ContainsKey(name))
                    _columnNames.Add(name, new List<ColumnInfo>());
                _columnNames[name].Add(new ColumnInfo(i));
            }
        }

        public bool HasColumn(string name)
        {
            if (name == null)
                return false;
            return _columnNames.ContainsKey(name);
        }

        public int GetColumnIndex(string name)
        {
            if (name == null || !_columnNames.ContainsKey(name))
                return -1;
            return _columnNames[name].Select(c => c.Index).First();
        }

        public IReadOnlyList<int> GetColumnIndices(string name)
        {
            if (name == null)
                return new List<int>();
            return _columnNames.ContainsKey(name) ? _columnNames[name].Select(c => c.Index).ToList() : new List<int>();
        }

        public void MarkMapped(string name, int count = 1)
        {
            if (!_columnNames.ContainsKey(name))
                return;
            foreach (var column in _columnNames[name])
            {
                if (column.Mapped)
                    continue;
                column.Mapped = true;
                count--;
                if (count <= 0)
                    break;
            }
        }

        public bool IsMapped(string name)
        {
            if (!_columnNames.ContainsKey(name))
                return false;
            return _columnNames[name].Select(c => c.Mapped).FirstOrDefault();
        }

        public ParameterExpression AddVariable<T>(string name)
        {
            return AddVariable(typeof(T), name);
        }

        public ParameterExpression AddVariable(Type t, string name)
        {
            var variable = Expression.Variable(t, name);
            _variables.Add(variable);
            return variable;
        }

        public void AddStatement(Expression expr)
        {
            _statements.Add(expr);
        }

        public Func<IDataRecord, T> CompileLambda<T>()
        {
            var lambdaExpression = Expression.Lambda<Func<IDataRecord, T>>(Expression.Block(Parent, _variables, _statements), RecordParam);
            DumpCodeToDebugConsole(lambdaExpression);
            return lambdaExpression.Compile();
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
            var code = (string)debugViewProp.GetGetMethod(true).Invoke(expr, null);
            Debug.WriteLine(code);
        }

        public int GetNextVarNumber()
        {
            return _varNumber++;
        }

        public IReadOnlyDictionary<string, int> GetColumnNameCounts()
        {
            return _columnNames.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);
        }
    }
}