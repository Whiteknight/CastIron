using System.Collections.Generic;
using System.Data;
using System.Linq;
using CastIron.Sql.Utility;

namespace CastIron.Sql.Mapping
{
    public class ColumnInfoCollection
    {
        private readonly Dictionary<string, List<ColumnInfo>> _columnNames;

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

        public ColumnInfoCollection()
        {
            _columnNames = new Dictionary<string, List<ColumnInfo>>();
        }

        public ColumnInfoCollection ForPrefix(string prefix)
        {
            if (prefix == null)
                return this;
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

        public IReadOnlyDictionary<string, int> GetColumnNameCounts()
            => _columnNames.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);
    }
}