﻿using System.Reflection;

namespace CastIron.Sql.Mapping
{
    public static class MemberInfoExtensions
    {
        public static bool IsSettable(this PropertyInfo property)
        {
            return property.SetMethod != null && property.CanWrite && !property.SetMethod.IsPrivate;
        }
    }
}