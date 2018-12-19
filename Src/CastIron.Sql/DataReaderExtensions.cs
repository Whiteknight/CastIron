using System;
using System.Data;

namespace CastIron.Sql
{
    // Helper methods to interact with IDatarecord
    // Some of these methods are inefficient for repeat use, and some kind of
    // compiled mapper would be better
    public static class DataReaderExtensions
    {
        public static string GetDataTypeName(this IDataRecord record, string columnName)
        {
            var index = record.GetOrdinal(columnName);
            return record.GetDataTypeName(index);
        }

        public static Type GetFieldtype(this IDataRecord record, string columnName)
        {
            var index = record.GetOrdinal(columnName);
            return record.GetFieldType(index);
        }

        public static object GetValue(this IDataRecord record, string columnName)
        {
            var index = record.GetOrdinal(columnName);
            return record.GetValue(index);
        }

        public static bool GetBoolean(this IDataRecord record, string columnName)
        {
            var index = record.GetOrdinal(columnName);
            return record.GetBoolean(index);
        }

        public static byte GetByte(this IDataRecord record, string columnName)
        {
            var index = record.GetOrdinal(columnName);
            return record.GetByte(index);
        }

        public static long GetBytes(this IDataRecord record, string columnName, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            var index = record.GetOrdinal(columnName);
            return record.GetBytes(index, fieldOffset, buffer, bufferoffset, length);
        }

        public static char GetChar(this IDataRecord record, string columnName)
        {
            var index = record.GetOrdinal(columnName);
            return record.GetChar(index);
        }

        public static long GetChars(this IDataRecord record, string columnName, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            var index = record.GetOrdinal(columnName);
            return record.GetChars(index, fieldoffset, buffer, bufferoffset, length);
        }

        public static Guid GetGuid(this IDataRecord record, string columnName)
        {
            var index = record.GetOrdinal(columnName);
            return record.GetGuid(index);
        }

        public static short GetInt16(this IDataRecord record, string columnName)
        {
            var index = record.GetOrdinal(columnName);
            return record.GetInt16(index);
        }

        public static int GetInt32(this IDataRecord record, string columnName)
        {
            var index = record.GetOrdinal(columnName);
            return record.GetInt32(index);
        }

        public static long GetInt64(this IDataRecord record, string columnName)
        {
            var index = record.GetOrdinal(columnName);
            return record.GetInt64(index);
        }

        public static float GetFloat(this IDataRecord record, string columnName)
        {
            var index = record.GetOrdinal(columnName);
            return record.GetFloat(index);
        }

        public static double GetDouble(this IDataRecord record, string columnName)
        {
            var index = record.GetOrdinal(columnName);
            return record.GetDouble(index);
        }

        public static string GetString(this IDataRecord record, string columnName)
        {
            var index = record.GetOrdinal(columnName);
            return record.GetString(index);
        }

        public static decimal GetDecimal(this IDataRecord record, string columnName)
        {
            var index = record.GetOrdinal(columnName);
            return record.GetDecimal(index);
        }

        public static DateTime GetDateTime(this IDataRecord record, string columnName)
        {
            var index = record.GetOrdinal(columnName);
            return record.GetDateTime(index);
        }

        public static bool IsDBNull(this IDataRecord record, string columnName)
        {
            var index = record.GetOrdinal(columnName);
            return record.IsDBNull(index);
        }

        public static IDataReader GetData(this IDataRecord record, string columnName)
        {
            var index = record.GetOrdinal(columnName);
            return record.GetData(index);
        }
    }
}