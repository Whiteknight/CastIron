using System;
using System.Collections.Generic;

namespace CastIron.Sql
{
    public abstract class Parameterized : ISqlParameterized
    {
        private readonly List<Parameter> _parameters;

        protected Parameterized()
        {
            _parameters = new List<Parameter>();
        }

        public IEnumerable<Parameter> GetParameters()
        {
            return _parameters;
        }

        public void AddParameter(Parameter parameter)
        {
            if (parameter == null)
                throw new ArgumentNullException(nameof(parameter));
            _parameters.Add(parameter);
        }
    }
}