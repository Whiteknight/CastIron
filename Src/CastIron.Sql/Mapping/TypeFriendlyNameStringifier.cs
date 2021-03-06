﻿using System;
using System.Linq;
using System.Text;

namespace CastIron.Sql.Mapping
{
    /// <summary>
    /// Tries to produce a more succinct, print-friendly, common stringification for a Type name
    /// than anything the built-in System.Type offers by default. The string representation of the
    /// type generated here is not round-trip friendly in a strict sense, but should be enough
    /// to help a programmer figure out what's going on.
    /// </summary>
    public class TypeFriendlyNameStringifier
    {
        public static TypeFriendlyNameStringifier Instance { get; } = new TypeFriendlyNameStringifier();

        public string GetFriendlyName(Type t)
        {
            if (t == null)
                return "NULL";
            var sb = new StringBuilder();
            GetFriendlyName(t, sb);
            return sb.ToString();
        }

        private static void GetFriendlyName(Type t, StringBuilder sb)
        {
            if (t.IsArray)
            {
                GetFriendlyNameForArrayType(t, sb);
                return;
            }

            var name = t.Name;
            if (name.Contains("`"))
                name = name.Split('`')[0];
            sb.Append(t.Namespace);
            sb.Append(".");
            sb.Append(name);
            if (t.IsGenericTypeDefinition)
                GetFriendlyNameForGenericTypeDefinition(sb, t);
            else if (t.IsConstructedGenericType)
                GetFriendlyNameForConstructedGenericType(sb, t);
        }

        private static void GetFriendlyNameForArrayType(Type t, StringBuilder sb)
        {
            var elementType = t.GetElementType();
            GetFriendlyName(elementType, sb);
            var commas = string.Join(",", Enumerable.Range(0, t.GetArrayRank()).Select(x => ""));
            sb.Append("[");
            sb.Append(commas);
            sb.Append("]");
        }

        private static void GetFriendlyNameForGenericTypeDefinition(StringBuilder sb, Type t)
        {
            var parameters = t.GetGenericArguments().Select((x, i) => "");
            sb.Append("<");
            sb.Append(string.Join(",", parameters));
            sb.Append(">");
        }

        private static void GetFriendlyNameForConstructedGenericType(StringBuilder sb, Type t)
        {
            sb.Append("<");
            var parameters = t.GetGenericArguments().Select(a => a.Namespace + "." + a.Name);
            sb.Append(string.Join(",", parameters));
            sb.Append(">");
        }
    }
}