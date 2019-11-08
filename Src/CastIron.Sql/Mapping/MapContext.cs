using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
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
    public struct MapContext
    {
        private readonly MapCompileOperation _operation;
        private readonly Dictionary<string, List<ColumnInfo>> _columnNames;

        public MapContext(MapCompileOperation operation, Dictionary<string, List<ColumnInfo>> columnNames, Type targetType, string name, Expression getExisting)
        {
            // TODO: We should probably also store columns in an ordered list, so we don't have to 
            // serialize/order for some of the options below.
            _operation = operation;
            _columnNames = columnNames;
            TargetType = targetType;
            Name = name;
            GetExisting = getExisting;
        }

        public string Name { get; }
        public Type TargetType { get; }
        public Expression GetExisting { get; }

        public ParameterExpression RecordParameter => _operation.RecordParam;
        
        public string Separator => _operation.Separator;

        public ParameterExpression CreateVariable<T>(string name) => _operation.CreateVariable<T>(name);
        
        public ParameterExpression CreateVariable(Type t, string name) => _operation.CreateVariable(t, name);

        public ColumnInfo SingleColumn() 
            => AllUnmappedColumns.OrderBy(c => c.Index).FirstOrDefault();

        public IEnumerable<ColumnInfo> GetColumns() => AllUnmappedColumns.OrderBy(c => c.Index);
        
        public IEnumerable<ColumnInfo> GetFirstIndexForEachColumnName()
            => _columnNames
                .Select(kvp => kvp.Value.FirstOrDefault(c => !c.Mapped))
                .Where(c => c != null);

        public bool HasColumn(string name) 
            =>  _columnNames.ContainsKey(name) && _columnNames[name].Any(c => !c.Mapped);

        public bool HasColumns() => AllUnmappedColumns.Any();

        public int NumberOfColumns() => AllUnmappedColumns.Count();

        public MapContext GetSubstateForColumn(ColumnInfo column, Type targetType, string name)
        {
            var columnNames = new Dictionary<string, List<ColumnInfo>>
            {
                { name?.ToLowerInvariant() ?? column.CanonicalName, new List<ColumnInfo> { column } }
            };
            return new MapContext(_operation, columnNames, targetType, name, null);
        }

        public MapContext GetSubstateForProperty(string name, ICustomAttributeProvider attrs, Type targetType, Expression getExisting = null)
        {
            var context = _operation;
            var columns = GetColumnsForProperty(name, attrs);
            return new MapContext(context, columns, targetType, name, getExisting);
        }

        public MapContext ChangeTargetType(Type targetType) => new MapContext(_operation, _columnNames, targetType, Name, GetExisting);

        public Func<object> GetFactoryForCurrentObjectType() => _operation.GetFactoryMethod(TargetType);

        public ConstructorInfo GetConstructorForCurrentObjectType()
        {
            var columnNameCounts = _columnNames.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);
            return _operation.GetConstructor(columnNameCounts, TargetType);
        }

        private IEnumerable<ColumnInfo> AllUnmappedColumns => _columnNames.SelectMany(kvp => kvp.Value).Where(c => !c.Mapped);

        private bool HasColumnsPrefixed(string prefix)
        {
            prefix += _operation.Separator;
            return _columnNames.Any(kvp => kvp.Key.StartsWith(prefix) && kvp.Value.Any(c => !c.Mapped));
        }

        private Dictionary<string, List<ColumnInfo>> GetColumnsPrefixed(string prefix)
        {
            prefix += _operation.Separator;
            return _columnNames
                .Where(kvp => kvp.Key.StartsWith(prefix))
                .ToDictionary(columns => columns.Key.Substring(prefix.Length), columns => columns.Value);
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
            if (acceptsUnnamed && _operation.Provider.UnnamedColumnName != null)
            {
                var unnamedName = _operation.Provider.UnnamedColumnName.ToLowerInvariant();
                if (HasColumn(unnamedName))
                    return new Dictionary<string, List<ColumnInfo>> { { unnamedName, _columnNames[unnamedName] } };
            }

            return new Dictionary<string, List<ColumnInfo>>();
        }
    }
}
