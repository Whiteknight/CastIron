namespace CastIron.Sql.Utility
{
    public static class StringExtensions
    {
        public static bool HasNonTrivialPrefix(this string s, string prefix)
        {
            if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(prefix))
                return false;

            return s.StartsWith(prefix) && s.Length > prefix.Length;
        }
    }
}
