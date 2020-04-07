using System;
using System.Data;

namespace CastIron.Sql.Debugging
{
    /// <summary>
    /// IDataReader implementation which tries to provide slightly more helpful error messages
    /// when something goes wrong.
    /// </summary>
    public sealed class DataReaderWithBetterErrorMessages : IDataReader
    {
        private readonly IDataReader _inner;

        public DataReaderWithBetterErrorMessages(IDataReader inner)
        {
            _inner = inner;
        }

        public void Dispose() => _inner.Dispose();

        public string GetName(int i) => _inner.GetName(i);

        public string GetDataTypeName(int i) => _inner.GetDataTypeName(i);

        public Type GetFieldType(int i) => _inner.GetFieldType(i);

        public object GetValue(int i) => _inner.GetValue(i);

        public int GetValues(object[] values) => _inner.GetValues(values);

        public int GetOrdinal(string name) => _inner.GetOrdinal(name);

        public bool GetBoolean(int i)
        {
            ThrowIfNullInsteadOfExpected<bool>(i);
            return ExpectType<bool>(i);
        }

        public byte GetByte(int i)
        {
            ThrowIfNullInsteadOfExpected<byte>(i);
            return ExpectType<byte>(i);
        }

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length) 
            => _inner.GetBytes(i, fieldOffset, buffer, bufferoffset, length);

        public char GetChar(int i)
        {
            ThrowIfNullInsteadOfExpected<char>(i);
            return ExpectType<char>(i);
        }

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length) 
            => _inner.GetChars(i, fieldoffset, buffer, bufferoffset, length);

        public Guid GetGuid(int i)
        {
            ThrowIfNullInsteadOfExpected<Guid>(i);
            return ExpectType<Guid>(i);
        }

        public short GetInt16(int i)
        {
            ThrowIfNullInsteadOfExpected<short>(i);
            return ExpectType<short>(i);
        }

        public int GetInt32(int i)
        {
            ThrowIfNullInsteadOfExpected<int>(i);
            return ExpectType<int>(i);
        }

        public long GetInt64(int i)
        {
            ThrowIfNullInsteadOfExpected<long>(i);
            return ExpectType<long>(i);
        }

        public float GetFloat(int i)
        {
            ThrowIfNullInsteadOfExpected<float>(i);
            return ExpectType<float>(i);
        }

        public double GetDouble(int i)
        {
            ThrowIfNullInsteadOfExpected<double>(i);
            return ExpectType<double>(i);
        }

        public string GetString(int i)
        {
            ThrowIfNullInsteadOfExpected<string>(i);
            return ExpectType<string>(i);
        }

        public decimal GetDecimal(int i)
        {
            ThrowIfNullInsteadOfExpected<decimal>(i);
            return ExpectType<decimal>(i);
        }

        public DateTime GetDateTime(int i)
        {
            ThrowIfNullInsteadOfExpected<DateTime>(i);
            return ExpectType<DateTime>(i);
        }

        public IDataReader GetData(int i) => _inner.GetData(i);

        public bool IsDBNull(int i) => _inner.IsDBNull(i);

        public int FieldCount => _inner.FieldCount;

        public object this[int i] => _inner[i];

        public object this[string name] => _inner[name];

        public void Close() => _inner.Close();

        public DataTable GetSchemaTable() => _inner.GetSchemaTable();

        public bool NextResult() => _inner.NextResult();

        public bool Read() => _inner.Read();

        public int Depth => _inner.Depth;
        public bool IsClosed => _inner.IsClosed;
        public int RecordsAffected => _inner.RecordsAffected;

        private void ThrowIfNullInsteadOfExpected<TExpected>(int i)
        {
            if (_inner.IsDBNull(i))
                throw new SqlQueryException($"Value at column {i} is DBNull but expected value of type {typeof(TExpected).Namespace}.{typeof(TExpected).Name}");
        }

        private T ExpectType<T>(int i)
        {
            var obj = GetValue(i);
            if (!(obj is T))
                throw new InvalidCastException($"Could not cast value in column {i} from type {i.GetType().Namespace}.{i.GetType().Name} to {typeof(T).Namespace}.{typeof(T).Name}");
            return (T)obj;
        }
    }
}