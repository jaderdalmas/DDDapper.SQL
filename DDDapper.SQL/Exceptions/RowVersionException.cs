using System;

namespace Back.Exceptions
{
    public class RowVersionException : Exception
    {
        public RowVersionException() : base("RowVersion!") { }
    }
}
