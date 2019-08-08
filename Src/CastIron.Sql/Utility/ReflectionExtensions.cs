using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CastIron.Sql.Utility
{
    public static class ReflectionExtensions 
    {
        public static IEnumerable<T> GetTypedAttributes<T>(this ICustomAttributeProvider reflectable, bool inherit = true)
            where T : Attribute
        {
            Argument.NotNull(reflectable, nameof(reflectable));
            return reflectable.GetCustomAttributes(typeof(T), inherit).Cast<T>();
        }
    }
}
