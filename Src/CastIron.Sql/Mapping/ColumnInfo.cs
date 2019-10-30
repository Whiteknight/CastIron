using System;

namespace CastIron.Sql.Mapping
{
    public class ColumnInfo
    {
        public ColumnInfo(int index, string originalName, Type columnType, string sqlTypeName)
        {
            Index = index;
            OriginalName = originalName;
            ColumnType = columnType;
            SqlTypeName = sqlTypeName;
            Mapped = false;
            CanonicalName = originalName.ToLowerInvariant();
        }

        public int Index { get; }
        public string OriginalName { get; }
        public string CanonicalName { get; }
        public Type ColumnType { get; }
        public string SqlTypeName { get; }

        public bool Mapped { get; private set; }

        public void MarkMapped()
        {
            Mapped = true;
        }
    }
}