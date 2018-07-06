using System;

namespace Back.Exceptions
{
    public class NoRowAffectedException : Exception
    {
        public NoRowAffectedException() : base("No Row Affected") { }
    }
}
