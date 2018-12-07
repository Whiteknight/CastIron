using System;

namespace CastIron.Sql
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    public sealed class UnnamedColumnsAttribute : Attribute
    {
    }
}
