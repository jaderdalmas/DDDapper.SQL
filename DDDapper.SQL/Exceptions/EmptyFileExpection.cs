using System;

namespace Back.Exceptions
{
    public class EmptyFileExpection : EmptyExpectedExpection
    {
        public EmptyFileExpection(String containerName) : base("FileName cannot be empty", containerName) { }
    }
}
