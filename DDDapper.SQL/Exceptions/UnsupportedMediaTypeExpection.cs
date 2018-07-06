using System;

namespace Back.Exceptions
{
    public class UnsupportedMediaTypeExpection : Exception
    {
        public UnsupportedMediaTypeExpection() : base("HttpCode: 415") { }
    }
}
