using System;

namespace CastIron.Sql
{
    /// <summary>
    /// Marks a property or constructor parameter as accepting values from unnamed columns
    /// This exists because ColumnAttribute cannot handle columns with null or empty names
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    public sealed class UnnamedColumnsAttribute : Attribute
    {
    }
}
