using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CastIron.Sql.Utility;

namespace CastIron.Sql.Mapping
{
    public struct MapState
    {
        private readonly MapCompileContext _context;
        private readonly ColumnInfoCollection _columns;
        private readonly List<string> _expressions;

        public MapState(MapCompileContext context, ColumnInfoCollection columns, Type targetType, string name, Expression getExisting)
        {
            _context = context;
            _columns = columns;
            _expressions = new List<string>();
            TargetType = targetType;
            Name = name;
            GetExisting = getExisting;
        }

        public string Name { get; }
        public Type TargetType { get; }
        public ParameterExpression RecordParameter => _context.RecordParam;
        public Expression GetExisting { get; set; }

        // TODO: CreateVariable should only create the variable not add it to a list.
        public ParameterExpression CreateVariable<T>(string name) => _context.CreateVariable<T>(name);
        public ParameterExpression CreateVariable(Type t, string name) => _context.CreateVariable(t, name);
        public ColumnInfo SingleColumn() => _columns.GetColumn();
        public IReadOnlyDictionary<string, int> GetColumnNameCounts() => _columns.GetColumnNameCounts();
        public IEnumerable<ColumnInfo> GetColumns() => _columns.GetColumns(null);
        public int NumUnmappedColumns() => _columns.NumUnmappedColumns();

        public IEnumerable<ColumnInfo> GetFirstIndexForEachColumnName() => _columns.GetFirstIndexForEachColumnName();
        public bool HasColumn(string name) => _columns.HasColumn(name);
        public bool HasColumns() => _columns.HasColumns();

        public MapState GetSubstateForColumn(ColumnInfo column, Type targetType, string name)
        {
            var columns = new ColumnInfoCollection(column);
            return new MapState(_context, columns, targetType, name, null);
        }

        public MapState GetSubstateForPrefix(string prefix, Type targetType, Expression getExisting = null)
        {
            var columns = prefix == null ? _columns : _columns.ForPrefix(prefix);
            return new MapState(_context, columns, targetType, prefix, getExisting);
        }

        public MapState GetSubstateForProperty(string name, ICustomAttributeProvider attrs, Type targetType, Expression getExisting = null)
        {
            var context = _context;
            var columns = GetColumnsForProperty(name, attrs);
            var columnCollection = new ColumnInfoCollection(columns);
            return new MapState(context, columnCollection, targetType, name, getExisting);
        }

        public MapState ChangeTargetType(Type targetType)
        {
            return new MapState(_context, _columns, targetType, Name, GetExisting);
        }

        public Func<object> GetFactoryForCurrentObjectType()
        {
            // TODO: Need to store factories by object type
            // we don't want to use the same factory for every custom object in the entire tree
            return _context.CreatePreferences.Factory;
        }

        public ConstructorInfo GetConstructorForCurrentObjectType()
        {
            // TODO: Need to store constructors by object type
            return _context.GetConstructor(TargetType);
        }

        private IEnumerable<ColumnInfo> GetColumnsForProperty(string name, ICustomAttributeProvider attrs)
        {
            if (name == null)
                return _columns.GetColumns(null);

            var propertyName = name.ToLowerInvariant();
            if (_columns.HasColumn(propertyName))
                return _columns.GetColumns(propertyName);
            if (_columns.HasColumnsPrefixed(propertyName))
                return _columns.GetColumnsPrefixed(propertyName);

            if (attrs == null)
                return Enumerable.Empty<ColumnInfo>();

            var columnName = attrs.GetTypedAttributes<ColumnAttribute>()
                .Select(c => c.Name.ToLowerInvariant())
                .FirstOrDefault();
            if (_columns.HasColumn(columnName))
                return _columns.GetColumns(columnName);

            var acceptsUnnamed = attrs.GetTypedAttributes<UnnamedColumnsAttribute>().Any();
            if (acceptsUnnamed && _context.Provider.UnnamedColumnName != null)
                return _columns.GetColumns(_context.Provider.UnnamedColumnName.ToLowerInvariant());
            return Enumerable.Empty<ColumnInfo>();
        }
    }
}
