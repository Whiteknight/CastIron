using System.Collections.Generic;

namespace CastIron.Sql
{
    public class ConnectionStrings
    {
        public const string ReadOnly = "_CI_RO";
        private readonly string _defaultConnectionString;
        private readonly Dictionary<string, string> _strings;

        public ConnectionStrings(string defaultConnectionString, string roConnectionString = null)
        {
            Assert.ArgumentNotNullOrEmpty(defaultConnectionString, nameof(defaultConnectionString));
            _defaultConnectionString = defaultConnectionString;
            _strings = new Dictionary<string, string> {
                { ReadOnly,  string.IsNullOrEmpty(roConnectionString) ? _defaultConnectionString : roConnectionString }
            };
        }

        public string Get(string name)
        {
            if (string.IsNullOrEmpty(name))
                return _defaultConnectionString;
            return _strings.ContainsKey(name) ? _strings[name] : _defaultConnectionString;
        }

        public string GetReadOnly()
        {
            return Get(ReadOnly);
        }

        public ConnectionStrings Add(string name, string connectionString)
        {
            if (_strings.ContainsKey(name))
                _strings[name] = connectionString;
            else
                _strings.Add(name, connectionString);
            return this;
        }
    }
}