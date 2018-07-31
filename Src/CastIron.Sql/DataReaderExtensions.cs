using System;
using System.Data;

namespace CastIron.Sql
{
    // Helper methods to interact with IDataReader
    // Some of these methods are inefficient for repeat use, and some kind of
    // compiled mapper would be better
    public static class DataReaderExtensions
    {
        public static string GetDataTypeName(this IDataRecord reader, string columnName)
        {
            var index = reader.GetOrdinal(columnName);
            return reader.GetDataTypeName(index);
        }

        public static Type GetFieldtype(this IDataRecord reader, string columnName)
        {
            var index = reader.GetOrdinal(columnName);
            return reader.GetFieldType(index);
        }

        public static object GetValue(this IDataRecord reader, string columnName)
        {
            var index = reader.GetOrdinal(columnName);
            return reader.GetValue(index);
        }

        public static bool GetBoolean(this IDataRecord reader, string columnName)
        {
            var index = reader.GetOrdinal(columnName);
            return reader.GetBoolean(index);
        }

        public static byte GetByte(this IDataRecord reader, string columnName)
        {
            var index = reader.GetOrdinal(columnName);
            return reader.GetByte(index);
        }

        public static long GetBytes(this IDataRecord reader, string columnName, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            var index = reader.GetOrdinal(columnName);
            return reader.GetBytes(index, fieldOffset, buffer, bufferoffset, length);
        }

        public static char GetChar(this IDataRecord reader, string columnName)
        {
            var index = reader.GetOrdinal(columnName);
            return reader.GetChar(index);
        }

        public static long GetChars(this IDataRecord reader, string columnName, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            var index = reader.GetOrdinal(columnName);
            return reader.GetChars(index, fieldoffset, buffer, bufferoffset, length);
        }

        public static Guid GetGuid(this IDataRecord reader, string columnName)
        {
            var index = reader.GetOrdinal(columnName);
            return reader.GetGuid(index);
        }

        public static short GetInt16(this IDataRecord reader, string columnName)
        {
            var index = reader.GetOrdinal(columnName);
            return reader.GetInt16(index);
        }

        public static int GetInt32(this IDataRecord reader, string columnName)
        {
            var index = reader.GetOrdinal(columnName);
            return reader.GetInt32(index);
        }

        public static long GetInt64(this IDataRecord reader, string columnName)
        {
            var index = reader.GetOrdinal(columnName);
            return reader.GetInt64(index);
        }

        public static float GetFloat(this IDataRecord reader, string columnName)
        {
            var index = reader.GetOrdinal(columnName);
            return reader.GetFloat(index);
        }

        public static double GetDouble(this IDataRecord reader, string columnName)
        {
            var index = reader.GetOrdinal(columnName);
            return reader.GetDouble(index);
        }

        public static string GetString(this IDataRecord reader, string columnName)
        {
            var index = reader.GetOrdinal(columnName);
            return reader.GetString(index);
        }

        public static decimal GetDecimal(this IDataRecord reader, string columnName)
        {
            var index = reader.GetOrdinal(columnName);
            return reader.GetDecimal(index);
        }

        public static DateTime GetDateTime(this IDataRecord reader, string columnName)
        {
            var index = reader.GetOrdinal(columnName);
            return reader.GetDateTime(index);
        }

        public static bool IsDBNull(this IDataRecord reader, string columnName)
        {
            var index = reader.GetOrdinal(columnName);
            return reader.IsDBNull(index);
        }

        public static IDataReader GetData(this IDataRecord reader, string columnName)
        {
            var index = reader.GetOrdinal(columnName);
            return reader.GetData(index);
        }
    }
}