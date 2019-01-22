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

    public class ColumnInfo
    {
        public ColumnInfo(int index, string originalName, Type columnType)
        {
            Index = index;
            OriginalName = originalName;
            ColumnType = columnType;
            Mapped = false;
            CanonicalName = originalName.ToLowerInvariant();
        }

        public int Index { get; }
        public string OriginalName { get; }
        public string CanonicalName { get; }
        public Type ColumnType { get; }

        public bool Mapped { get; private set; }

        public void MarkMapped()
        {
            Mapped = true;
        }
    }

    public class MapCompileContext
    {
        private readonly IConstructorFinder _constructorFinder;
        private readonly Dictionary<string, List<ColumnInfo>> _columnNames;
        private readonly List<ParameterExpression> _variables;
        private readonly List<Expression> _statements;
        private readonly VariableNumberSource _variableNumbers;
        private readonly string _separator;

        public MapCompileContext(IDataReader reader, Type parent, Type specific, Func<object> factory, ConstructorInfo preferredConstructor, IConstructorFinder constructorFinder, VariableNumberSource numberSource = null, string separator = "_")
        {
            Assert.ArgumentNotNull(reader, nameof(reader));
            Assert.ArgumentNotNull(parent, nameof(parent));

            _separator = (separator ?? "_").ToLowerInvariant();
            Parent = parent;
            Specific = specific ?? parent;

            Factory = factory;

            Reader = reader;
            RecordParam = Expression.Parameter(typeof(IDataRecord), "record");
            
            PreferredConstructor = preferredConstructor;
            _constructorFinder = constructorFinder;
            
            _variableNumbers = numberSource ?? new VariableNumberSource();
            _variables = new List<ParameterExpression>();
            _statements = new List<Expression>();
            _columnNames = new Dictionary<string, List<ColumnInfo>>();

            var name = $"instance_{GetNextVarNumber()}";
            Instance = Expression.Variable(Specific, name);
            _variables.Add(Instance);
        }

        public MapCompileContext(MapCompileContext parent, Type type, string prefix, string separator)
        {
            Assert.ArgumentNotNull(parent, nameof(parent));
            Assert.ArgumentNotNullOrEmpty(separator, nameof(_separator));

            _separator = separator;
            Parent = type;
            Specific = type;

            Reader = parent.Reader;
            RecordParam = parent.RecordParam;
            
            _constructorFinder = parent._constructorFinder;

            _variableNumbers = parent._variableNumbers;
            _variables = parent._variables;
            _statements = parent._statements;
            _columnNames = new Dictionary<string, List<ColumnInfo>>();
            if (prefix == null)
                _columnNames = parent._columnNames;
            else
            {
                foreach (var columnName in parent._columnNames)
                {
                    if (columnName.Key.HasNonTrivialPrefix(prefix))
                    {
                        var newName = columnName.Key.Substring(prefix.Length);
                        _columnNames.Add(newName, columnName.Value);
                    }
                }
            }

            if (type != null)
            {
                var name = $"instance_{GetNextVarNumber()}";
                Instance = Expression.Variable(Specific, name);
                _variables.Add(Instance);
            }
        }

        public Func<object> Factory { get; }
        public IDataReader Reader { get; }
        public ParameterExpression RecordParam { get; }
        public ParameterExpression Instance { get; }
        public Type Parent { get; }
        public Type Specific { get; }

        //public List<ParameterExpression> Variables { get; }
        //public List<Expression> Statements { get; }

        public MapCompileContext CreateSubcontext(Type t, string prefix)
        {
            if (!string.IsNullOrEmpty(prefix))
                prefix = prefix + _separator;
            return new MapCompileContext(this, t, prefix, _separator);
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
                // TODO: Specify list of prefixes to ignore. If the column starts with a prefix, remove those chars from the front
                // e.g. "Table_ID" with prefix "Table_" becomes "ID"
                var name = (reader.GetName(i) ?? "");
                var info = new ColumnInfo(i, name, reader.GetFieldType(i));
                if (!_columnNames.ContainsKey(info.CanonicalName))
                    _columnNames.Add(info.CanonicalName, new List<ColumnInfo>());
                _columnNames[info.CanonicalName].Add(info);
            }
        }

        public bool HasColumn(string name)
        {
            if (name == null)
                return false;
            return _columnNames.ContainsKey(name);
        }

        public ColumnInfo GetColumn(string name)
        {
            if (name == null)
                return _columnNames.SelectMany(kvp => kvp.Value).FirstOrDefault(c => !c.Mapped);
            if (!_columnNames.ContainsKey(name))
                return null;
            return _columnNames[name].FirstOrDefault(c => !c.Mapped);
        }

        public IEnumerable<ColumnInfo> GetColumns(string name)
        {
            if (name == null)
                return _columnNames.SelectMany(kvp => kvp.Value).Where(c => !c.Mapped);
            return _columnNames.ContainsKey(name) ? _columnNames[name].Where(c => !c.Mapped) : Enumerable.Empty<ColumnInfo>();
        }

        public IEnumerable<ColumnInfo> GetFirstIndexForEachColumnName()
        {
            return _columnNames
                .Select(kvp => kvp.Value.FirstOrDefault(c => !c.Mapped))
                .Where(c => c != null);
        }

        public ParameterExpression AddVariable<T>(string name)
        {
            return AddVariable(typeof(T), name);
        }

        public ParameterExpression AddVariable(Type t, string name)
        {
            name = name + "_" + GetNextVarNumber();
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