using System;

namespace Back.Exceptions
{
    public class NoContentException : Exception
    {
        public NoContentException() : base("HttpCode: 204") { }
    }
}
