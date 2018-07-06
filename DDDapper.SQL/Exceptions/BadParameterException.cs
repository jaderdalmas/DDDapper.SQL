using System;

namespace Back.Exceptions
{
    public class BadParameterException : Exception
    {
        public BadParameterException() : base("Missing or Invalid Parameter!") { }
    }
}
