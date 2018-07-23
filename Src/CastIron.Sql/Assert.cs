using System;

namespace CastIron.Sql
{
    internal static class Assert
    {
        public static void ArgumentNotNull(object value, string name)
        {
            if (value == null)
                throw new ArgumentNullException(name);
        }
    }
}
