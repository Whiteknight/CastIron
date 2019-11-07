using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CastIron.Sql.Utility;

namespace CastIron.Sql.Mapping
{
    /// <summary>
    /// Holds information about the current state of the map compilation, including the current target type
    /// to construct and a list of columns available at this point. This type is a flyweight, delegating to
    /// the MapCompileContext for all shared data and logic
    /// </summary>
    public struct MapState
    {
        private readonly MapCompileContext _context;
        private readonly Dictionary<string, List<ColumnInfo>> _columnNames;

        // TODO: Need to take the accumulated prefixes of all parent states, so we can correctly get short
        // names for columns at any level of nesting.
        public MapState(MapCompileContext context, Dictionary<string, List<ColumnInfo>> columnNames, Type targetType, string name, Expression getExisting)
        {
            _context = context;
            _columnNames = columnNames;
            TargetType = targetType;
            Name = name;
            GetExisting = getExisting;
        }

        public static MapState FromDataReader(MapCompileContext context, Type targetType, IDataReader reader)
        {
            var columnNames = new Dictionary<string, List<ColumnInfo>>();
            // TODO: We should be able to calculate this ahead of time and inject the readonly dict in the constructor
            for (var i = 0; i < reader.FieldCount; i++)
            {
                // TODO: Specify list of prefixes to ignore. If the column starts with a prefix, remove those chars from the front
                // e.g. "Table_ID" with prefix "Table_" becomes "ID"
                var name = (reader.GetName(i) ?? "");
                var typeName = reader.GetDataTypeName(i);
                var columnType = reader.GetFieldType(i);
                var info = new ColumnInfo(i, name, columnType, typeName);
                if (!columnNames.ContainsKey(info.CanonicalName))
                    columnNames.Add(info.CanonicalName, new List<ColumnInfo>());
                columnNames[info.CanonicalName].Add(info);
            }

            return new MapState(context, columnNames, targetType, null, null);
        }

        public string Name { get; }
        public Type TargetType { get; }
        public ParameterExpression RecordParameter => _context.RecordParam;
        public Expression GetExisting { get; }
        public string Separator => _context.Separator;

        // TODO: CreateVariable should only create the variable not add it to a list.
        public ParameterExpression CreateVariable<T>(string name) => _context.CreateVariable<T>(name);
        public ParameterExpression CreateVariable(Type t, string name) => _context.CreateVariable(t, name);

        public ColumnInfo SingleColumn() 
            => _columnNames.SelectMany(kvp => kvp.Value).OrderBy(c => c.Index).FirstOrDefault(c => !c.Mapped);

        public IEnumerable<ColumnInfo> GetColumns() 
            => _columnNames.Values.SelectMany(v => v).Where(c => !c.Mapped).OrderBy(c => c.Index);
        
        public IEnumerable<ColumnInfo> GetFirstIndexForEachColumnName()
            => _columnNames
                .Select(kvp => kvp.Value.FirstOrDefault(c => !c.Mapped))
                .Where(c => c != null);

        public bool HasColumn(string name) 
            =>  _columnNames.ContainsKey(name) && _columnNames[name].Any(c => !c.Mapped);
        public bool HasColumns() => _columnNames.SelectMany(kvp => kvp.Value).Any(c => !c.Mapped);

        public int NumberOfColumns() => _columnNames.SelectMany(kvp => kvp.Value).Count(c => !c.Mapped);

        public MapState GetSubstateForColumn(ColumnInfo column, Type targetType, string name)
        {
            var columnNames = new Dictionary<string, List<ColumnInfo>>();
            columnNames.Add(column.CanonicalName, new List<ColumnInfo> { column });
            return new MapState(_context, columnNames, targetType, name, null);
        }

        public MapState GetSubstateForPrefix(string prefix, Type targetType, Expression getExisting = null)
        {
            var columns = _columnNames;
            if (prefix == null)
                return new MapState(_context, columns, targetType, null, getExisting);

            var prefixAndSeparator = prefix +_context.Separator;
            columns = new Dictionary<string, List<ColumnInfo>>();
            foreach (var columnName in _columnNames)
            {
                if (!columnName.Key.HasNonTrivialPrefix(prefix))
                    continue;
                var newName = columnName.Key.Substring(prefixAndSeparator.Length);
                columns.Add(newName, columnName.Value);
            }
            return new MapState(_context, columns, targetType, prefix, getExisting);
        }

        public MapState GetSubstateForProperty(string name, ICustomAttributeProvider attrs, Type targetType, Expression getExisting = null)
        {
            var context = _context;
            var columns = GetColumnsForProperty(name, attrs);
            return new MapState(context, columns, targetType, name, getExisting);
        }

        public MapState ChangeTargetType(Type targetType) => new MapState(_context, _columnNames, targetType, Name, GetExisting);

        public Func<object> GetFactoryForCurrentObjectType() => _context.GetFactoryMethod(TargetType);

        public ConstructorInfo GetConstructorForCurrentObjectType()
        {
            var columnNameCounts = _columnNames.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);
            return _context.GetConstructor(columnNameCounts, TargetType);
        }

        private bool HasColumnsPrefixed(string prefix)
        {
            prefix += _context.Separator;
            return _columnNames.Keys.Any(k => k.StartsWith(prefix));
        }

        private Dictionary<string, List<ColumnInfo>> GetColumnsPrefixed(string prefix)
        {
            prefix += _context.Separator;
            return _columnNames.Where(kvp => kvp.Key.StartsWith(prefix)).ToDictionary(columns => columns.Key, columns => columns.Value);
        }

        private Dictionary<string, List<ColumnInfo>> GetColumnsForProperty(string name, ICustomAttributeProvider attrs)
        {
            if (name == null)
                return _columnNames;

            var propertyName = name.ToLowerInvariant();
            if (HasColumn(propertyName))
                return new Dictionary<string, List<ColumnInfo>> { { propertyName, _columnNames[propertyName] } };
            if (HasColumnsPrefixed(propertyName))
                return GetColumnsPrefixed(propertyName);

            if (attrs == null)
                return new Dictionary<string, List<ColumnInfo>>();

            var columnName = attrs.GetTypedAttributes<ColumnAttribute>()
                .Select(c => c.Name.ToLowerInvariant())
                .FirstOrDefault();
            if (columnName != null && HasColumn(columnName))
                return new Dictionary<string, List<ColumnInfo>> { { columnName, _columnNames[columnName] } };

            var acceptsUnnamed = attrs.GetTypedAttributes<UnnamedColumnsAttribute>().Any();
            if (acceptsUnnamed && _context.Provider.UnnamedColumnName != null)
            {
                var unnamedName = _context.Provider.UnnamedColumnName.ToLowerInvariant();
                if (HasColumn(unnamedName))
                    return new Dictionary<string, List<ColumnInfo>> { { unnamedName, _columnNames[unnamedName] } };
            }

            return new Dictionary<string, List<ColumnInfo>>();
        }
    }
}
