using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CastIron.Sql.Commands
{
    public class SqlInsertCommand : ISqlCommand
    {
        private readonly string _tableName;

        public Dictionary<string, string> Values { get; }

        public static string Quote(string s)
        {   
            return "'" + s.Replace("'", "''") + "'";
        }

        public static string GetDateTimeString(DateTime value)
        {
            return string.Format("'{0:yyyy-MM-dd HH:mm:ss.fff}'", value);
        }

        public SqlInsertCommand(string tableName)
        {
            _tableName = tableName;
            Values = new Dictionary<string, string>();
        }

        public void Add(string name, string value)
        {
            AddColumnInternal(name, value == null ? "NULL" : Quote(value));
        }

        public void Add(string name, int? value)
        {
            AddColumnInternal(name, value == null ? "NULL" : value.ToString());
        }

        public void Add(string name, long? value)
        {
            AddColumnInternal(name, value == null ? "NULL" : value.ToString());
        }

        public void Add(string name, DateTime? value)
        {
            AddColumnInternal(name, value == null ? "NULL" : GetDateTimeString(value.Value));
        }

        public void Add(string name, Guid? value)
        {
            AddColumnInternal(name, value == null ? "NULL" : Quote(value.Value.ToString()));
        }

        public void Add(string name, bool? value)
        {
            AddColumnInternal(name, value == null ? "NULL" : (value.Value ? "1" : "0"));
        }

        private void AddColumnInternal(string name, string value)
        {
            if (string.IsNullOrEmpty(name))
                throw new Exception("Column must have a name");
            if (Values.ContainsKey(name))
                throw new Exception($"Already specified column {name} it may not be specified twice");
            Values.Add(name, value);
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            ToBuilder(builder);
            return builder.ToString();
        }

        public void ToBuilder(StringBuilder builder)
        {
            var values = Values.ToList();
            if (values.Count == 0)
                return;

            builder.Append("INSERT INTO ");
            builder.Append(_tableName);
            builder.Append(" (");
            builder.Append(string.Join(",", values.Select(v => v.Key)));
            builder.Append(") VALUES (");
            builder.Append(string.Join(",", values.Select(v => v.Value)));
            builder.Append(");");
        }

        public string GetSql()
        {
            return ToString();
        }
    }
}
