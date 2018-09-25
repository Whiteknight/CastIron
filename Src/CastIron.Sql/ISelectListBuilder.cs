using System;

namespace CastIron.Sql
{
    public interface ISelectListBuilder
    {
        string BuildSelectList(Type type, string prefix = "");
    }
}