using System.Reflection;

namespace CastIron.Sql.Mapping
{
    public static class MemberInfoExtensions
    {
        /// <summary>
        /// Determines if the property value can be set
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public static bool IsSettable(this PropertyInfo property) 
            => property.SetMethod != null && property.CanWrite && !property.SetMethod.IsPrivate;
    }
}
