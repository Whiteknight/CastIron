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
    public class MapCompileContext<T> : MapCompileContext
    {
        public Func<T> Factory { get; }

        public MapCompileContext(IDataReader reader, Type specific, Func<T> factory, ConstructorInfo preferredConstructor, IConstructorFinder constructorFinder) 
            : base(reader, typeof(T), specific, preferredConstructor, constructorFinder)
        {
            Factory = factory;
        }
    }

    public class VariableNumberSource
    {
        private int _varNumber;

        public VariableNumberSource()
        {
            _varNumber = 0;
        }

        public int GetNext()
        {
            return _varNumber++;
        }

    }

    public class MapCompileContext
    {
        private readonly IConstructorFinder _constructorFinder;
        private readonly string _prefix;
        private readonly Dictionary<string, List<ColumnInfo>> _columnNames;
        private readonly List<ParameterExpression> _variables;
        private readonly List<Expression> _statements;
        private readonly VariableNumberSource _variableNumbers;
        private readonly int _numColumns;

        private class ColumnInfo
        {
            public int Index { get; }

            public ColumnInfo(int index)
            {
                Index = index;
            }

            public bool Mapped { get; set; }
        }

        public MapCompileContext(IDataReader reader, Type parent, Type specific, ConstructorInfo preferredConstructor, IConstructorFinder constructorFinder, VariableNumberSource numberSource = null)
        {
            Assert.ArgumentNotNull(reader, nameof(reader));
            Assert.ArgumentNotNull(parent, nameof(parent));

            Parent = parent;
            Specific = specific ?? parent;

            Reader = reader;
            RecordParam = Expression.Parameter(typeof(IDataRecord), "record");
            
            PreferredConstructor = preferredConstructor;
            _constructorFinder = constructorFinder;
            
            _variableNumbers = numberSource ?? new VariableNumberSource();
            _variables = new List<ParameterExpression>();
            _statements = new List<Expression>();
            _columnNames = new Dictionary<string, List<ColumnInfo>>();
            _numColumns = reader.FieldCount;

            var name = $"instance_{GetNextVarNumber()}";
            Instance = Expression.Variable(Specific, name);
            _variables.Add(Instance);
        }

        public MapCompileContext(MapCompileContext parent, Type type, string prefix)
        {
            Assert.ArgumentNotNull(parent, nameof(parent));
            Assert.ArgumentNotNullOrEmpty(prefix, nameof(prefix));
            Assert.ArgumentNotNull(type, nameof(type));

            Parent = type;
            Specific = type;

            Reader = parent.Reader;
            RecordParam = parent.RecordParam;
            
            _constructorFinder = parent._constructorFinder;

            _variableNumbers = parent._variableNumbers;
            _variables = parent._variables;
            _statements = parent._statements;
            _columnNames = new Dictionary<string, List<ColumnInfo>>();
            foreach (var columnName in parent._columnNames)
            {
                if (columnName.Key.HasNonTrivialPrefix(prefix))
                {
                    var newName = columnName.Key.Substring(prefix.Length);
                    _columnNames.Add(newName, columnName.Value);
                }
            }
            //_numColumns = reader.FieldCount;

            var name = $"instance_{GetNextVarNumber()}";
            Instance = Expression.Variable(Specific, name);
            _variables.Add(Instance);
            _prefix = prefix;
        }
        
        public IDataReader Reader { get; }
        public ParameterExpression RecordParam { get; }
        public ParameterExpression Instance { get; }
        public Type Parent { get; }
        public Type Specific { get; }

        //public List<ParameterExpression> Variables { get; }
        //public List<Expression> Statements { get; }

        public MapCompileContext CreateSubcontext(Type t, string prefix)
        {
            return new MapCompileContext(this, t, prefix);
        }

        public ConstructorInfo PreferredConstructor { get; set; }

        public ConstructorInfo GetConstructor()
        {   
            return (_constructorFinder ?? ConstructorFinder.GetDefaultInstance()).FindBestMatch(PreferredConstructor, Specific, GetColumnNameCounts());
        }

        public void PopulateColumnLookups(IDataReader reader)
        {
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var name = (reader.GetName(i) ?? "").ToLowerInvariant();
                if (!string.IsNullOrEmpty(_prefix))
                {
                    if (!name.HasNonTrivialPrefix(_prefix))
                        continue;
                    name = name.Substring(_prefix.Length);
                }

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

        public IReadOnlyList<int> GetAllColumnIndices()
        {
            // TODO: Need to fix this, subcontexts won't be able to use this.
            return Enumerable.Range(0, _numColumns).ToList();
        }

        public IReadOnlyDictionary<string, IReadOnlyList<int>> GetColumnIndicesByPrefix(string prefix)
        {
            return _columnNames.Where(kvp => kvp.Key.HasNonTrivialPrefix(prefix))
                .ToDictionary(
                    kvp => kvp.Key, 
                    kvp => (IReadOnlyList<int>)kvp.Value
                        .Where(c => !c.Mapped)
                        .Select(c => c.Index)
                        .ToList());
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

        public void MarkMapped(string prefix, string name, int count = 1)
        {
            MarkMapped(prefix + name, count);
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
            return _variableNumbers.GetNext();
        }


        public IReadOnlyDictionary<string, int> GetColumnNameCounts()
        {
            return _columnNames.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);
        }
    }
}