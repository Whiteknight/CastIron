﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CastIron.Sql.Mapping.Constructors;
using CastIron.Sql.Utility;

namespace CastIron.Sql.Mapping
{
    /// <summary>
    /// Holds information about the current state of the map compilation, including the current target type
    /// to construct and a list of columns available at this point. This type is a flyweight, delegating to
    /// the MapCompileContext for all shared data and logic
    /// </summary>
    public struct MapTypeContext
    {
        private readonly MapContext _context;
        private readonly Dictionary<string, List<ColumnInfo>> _columnNames;

        public MapTypeContext(MapContext context, Dictionary<string, List<ColumnInfo>> columnNames, Type targetType, string name, string currentPrefix, Expression getExisting)
        {
            _context = context;
            _columnNames = columnNames;
            TargetType = targetType;
            Name = name;
            GetExisting = getExisting;
            CurrentPrefix = currentPrefix ?? "";
        }

        public string Name { get; }
        public string CurrentPrefix { get; }
        public Type TargetType { get; }
        public Expression GetExisting { get; }

        public ParameterExpression RecordParameter => _context.RecordParam;

        public string Separator => _context.Separator;

        public CompilationSettings Settings => _context.Settings;

        public ParameterExpression CreateVariable(Type t, string name)
            => _context.CreateVariable(t, name);

        public LabelExpression CreateLabel(string name) => _context.CreateLabel(name);

        public ColumnInfo SingleColumn()
            => AllUnmappedColumns.OrderBy(c => c.Index).FirstOrDefault();

        public IEnumerable<ColumnInfo> GetColumns() => AllUnmappedColumns.OrderBy(c => c.Index);

        public IEnumerable<ColumnInfo> GetFirstIndexForEachColumnName()
        {
            return _columnNames
                .Select(kvp => kvp.Value.FirstOrDefault(c => !c.Mapped))
                .Where(c => c != null);
        }

        public bool HasColumn(string name)
            => _columnNames.ContainsKey(name) && _columnNames[name].Any(c => !c.Mapped);

        public bool HasColumns() => AllUnmappedColumns.Any();

        public int NumberOfColumns() => AllUnmappedColumns.Count();

        public MapTypeContext GetSubstateForColumn(ColumnInfo column, Type targetType, string name)
        {
            var columnNames = new Dictionary<string, List<ColumnInfo>>
            {
                { name?.ToLowerInvariant() ?? column.CanonicalName, new List<ColumnInfo> { column } }
            };
            return new MapTypeContext(_context, columnNames, targetType, name, CurrentPrefix, null);
        }

        public MapTypeContext GetSubstateForProperty(string name, ICustomAttributeProvider attrs, Type targetType, Expression getExisting = null)
        {
            var context = _context;
            var columns = GetColumnsForProperty(name, attrs);
            var prefix = string.IsNullOrEmpty(name) ? CurrentPrefix : CurrentPrefix + name + Separator;
            return new MapTypeContext(context, columns, targetType, name, prefix, getExisting);
        }

        public MapTypeContext ChangeTargetType(Type targetType)
            => new MapTypeContext(_context, _columnNames, targetType, Name, CurrentPrefix, GetExisting);

        public ConstructorInfo GetConstructor(ISpecificTypeSettings settings)
        {
            var columnNameCounts = _columnNames.ToDictionary(k => k.Key, k => k.Value.Count);
            // If there are no settings configured, the settings.Type may default to typeof(object)
            // which is clearly not what we want. If we detect that situation, fall back to
            // TargetType
            var type = settings.Type == typeof(object) || settings.Type == null ? TargetType : settings.Type;
            if (type.IsAbstract || type.IsInterface)
                throw MapCompilerException.UnconstructableAbstractType(type);
            return (settings.ConstructorFinder ?? BestMatchConstructorFinder.GetDefaultInstance())
                .FindBestMatch(_context.Provider, settings.Constructor, type, columnNameCounts);
        }

        private IEnumerable<ColumnInfo> AllUnmappedColumns
            => _columnNames.SelectMany(kvp => kvp.Value).Where(c => !c.Mapped);

        private bool HasColumnsPrefixed(string prefix)
        {
            prefix += _context.Separator;
            return _columnNames.Any(kvp => kvp.Key.StartsWith(prefix) && kvp.Value.Any(c => !c.Mapped));
        }

        private Dictionary<string, List<ColumnInfo>> GetColumnsPrefixed(string prefix)
        {
            prefix += _context.Separator;
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
