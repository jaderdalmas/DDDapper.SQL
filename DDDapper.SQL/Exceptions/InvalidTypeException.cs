using System;

namespace Back.Exceptions
{
    public class InvalidTypeException : Exception
    {
        public InvalidTypeException() : base("Invalid Type!") { }
    }
}
