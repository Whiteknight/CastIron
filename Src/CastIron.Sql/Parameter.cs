using System.Data;

namespace CastIron.Sql
{
    public class Parameter
    {
        public Parameter(string name, object value, ParameterDirection direction = ParameterDirection.Input)
        {
            Name = name;
            Value = value;
            Direction = direction;
        }

        public string Name { get; }
        public object Value { get; }
        public ParameterDirection Direction { get; }
    }
}