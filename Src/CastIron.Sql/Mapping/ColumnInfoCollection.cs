using System.Collections.Generic;
using System.Data;
using System.Linq;
using CastIron.Sql.Utility;

namespace CastIron.Sql.Mapping
{
    public class ColumnInfoCollection
    {
        private readonly Dictionary<string, List<ColumnInfo>> _columnNames;
        // TODO: Inject this in all constructors
        private const string _separator = "_";


        public ColumnInfoCollection(IDataReader reader)
        {
            _columnNames = new Dictionary<string, List<ColumnInfo>>();
            // TODO: We should be able to calculate this ahead of time and inject the readonly dict in the constructor
            for (var i = 0; i < reader.FieldCount; i++)
            {
                // TODO: Specify list of prefixes to ignore. If the column starts with a prefix, remove those chars from the front
                // e.g. "Table_ID" with prefix "Table_" becomes "ID"
                var name = (reader.GetName(i) ?? "");
                var typeName = reader.GetDataTypeName(i);
                var columnType = reader.GetFieldType(i);
                var info = new ColumnInfo(i, name, columnType, typeName);
                if (!_columnNames.ContainsKey(info.CanonicalName))
                    _columnNames.Add(info.CanonicalName, new List<ColumnInfo>());
                _columnNames[info.CanonicalName].Add(info);
            }
        }

        public ColumnInfoCollection(ColumnInfo singleColumn)
        {
            _columnNames = new Dictionary<string, List<ColumnInfo>>();
            _columnNames.Add(singleColumn.CanonicalName, new List<ColumnInfo> { singleColumn });
        }

        public ColumnInfoCollection(IEnumerable<ColumnInfo> columns)
        {
            _columnNames = columns.GroupBy(c => c.CanonicalName).ToDictionary(g => g.Key, g => g.ToList());
        }

        public ColumnInfoCollection()
        {
            _columnNames = new Dictionary<string, List<ColumnInfo>>();
        }

        public ColumnInfoCollection ForPrefix(string prefix)
        {
            if (prefix == null)
                return this;
            prefix = prefix + _separator;
            var child = new ColumnInfoCollection();
            foreach (var columnName in _columnNames)
            {
                if (!columnName.Key.HasNonTrivialPrefix(prefix))
                    continue;
                var newName = columnName.Key.Substring(prefix.Length);
                child._columnNames.Add(newName, columnName.Value);
            }

            return child;
        }

        public ColumnInfoCollection ForName(string name)
        {
            if (name == null)
                return this;
            var columns = _columnNames.ContainsKey(name) ? _columnNames[name].Where(c => !c.Mapped).OrderBy(c => c.Index) : Enumerable.Empty<ColumnInfo>();
            return new ColumnInfoCollection(columns);
        }

        public bool HasColumn(string name)
        {
            if (name == null)
                return false;
            return _columnNames.ContainsKey(name);
        }

        public bool HasColumns() => _columnNames.SelectMany(kvp => kvp.Value).Any(c => !c.Mapped);

        public bool HasColumnsPrefixed(string prefix)
        {
            prefix = _separator + prefix;
            return _columnNames.Keys.Any(k => k.StartsWith(prefix));
        }

        public ColumnInfo GetColumn()
        {
            return _columnNames.SelectMany(kvp => kvp.Value).Where(c => !c.Mapped).OrderBy(c => c.Index).FirstOrDefault();
        }

        public IEnumerable<ColumnInfo> GetColumns(string name)
        {
            if (name == null)
                return _columnNames.Values.SelectMany(v => v).Where(c => !c.Mapped).OrderBy(c => c.Index);
            return _columnNames.ContainsKey(name) ? _columnNames[name].Where(c => !c.Mapped).OrderBy(c => c.Index) : Enumerable.Empty<ColumnInfo>();
        }

        public IEnumerable<ColumnInfo> GetColumnsPrefixed(string prefix)
        {
            prefix = _separator + prefix;
            return _columnNames.Where(kvp => kvp.Key.StartsWith(prefix)).SelectMany(kvp => kvp.Value).Where(c => !c.Mapped).OrderBy(c => c.Index);
        }

        public ColumnInfo GetColumn(string name)
        {
            if (name == null)
                return _columnNames.SelectMany(kvp => kvp.Value).FirstOrDefault(c => !c.Mapped);
            if (!_columnNames.ContainsKey(name))
                return null;
            return _columnNames[name].FirstOrDefault(c => !c.Mapped);
        }

        public IEnumerable<ColumnInfo> GetFirstIndexForEachColumnName()
        {
            return _columnNames
                .Select(kvp => kvp.Value.FirstOrDefault(c => !c.Mapped))
                .Where(c => c != null);
        }

        public IReadOnlyDictionary<string, int> GetColumnNameCounts()
            => _columnNames.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);

        public int NumUnmappedColumns() => _columnNames.SelectMany(kvp => kvp.Value).Count(c => !c.Mapped);
    }
}