using System;

namespace Back.Exceptions
{
    public class ForbiddenException : Exception
    {
        public ForbiddenException(String message = "") : base(message) { }
    }
}
