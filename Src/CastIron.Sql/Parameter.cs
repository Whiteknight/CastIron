using System;
using System.Data;

namespace CastIron.Sql
{
    public class Parameter
    {
        public Parameter(string name, object value, DbType type, int size, ParameterDirection direction = ParameterDirection.Input)
        {
            Name = name;
            Value = value;
            Type = type;
            Size = size;
            Direction = direction;
        }

        public static Parameter CreateStringInputParameter(string name, string value)
        {
            return new Parameter(name, value, DbType.AnsiString, value?.Length ?? 0, ParameterDirection.Input);
        }

        public static Parameter CreateStringOutputParameter(string name, int maxLength)
        {
            return new Parameter(name, null, DbType.AnsiString, maxLength, ParameterDirection.Output);
        }

        public static Parameter CreateStringOutputParameter(string name, string initValue, int maxLength)
        {
            initValue = initValue ?? "";
            return new Parameter(name, initValue, DbType.AnsiString, Math.Max(initValue.Length, maxLength), ParameterDirection.InputOutput);
        }

        public string Name { get; }
        public object Value { get; }
        public DbType Type { get; }
        public int Size { get; }
        public ParameterDirection Direction { get; }
    }
}