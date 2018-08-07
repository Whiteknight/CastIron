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

        public static void ArgumentNotNullOrEmpty(string value, string name)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentNullException(name, "Value must not be null or empty");
        }
    }
}
