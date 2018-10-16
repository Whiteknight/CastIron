using System;

namespace CastIron.Sql
{
    public static class CIAssert
    {
        public static void ArgumentNotNull(object value, string name)
        {
            if (value == null)
                throw new ArgumentNullException(name);
        }

        public static void ArgumentNotNullOrEmpty(string value, string name)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentNullException(name);
        }
    }
}
