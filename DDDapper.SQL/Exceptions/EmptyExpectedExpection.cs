using System;

namespace Back.Exceptions
{
    public class EmptyExpectedExpection : Exception
    {
        public EmptyExpectedExpection(String ex, String containerString) : base(ex + ", expected: " + containerString) { }
        public EmptyExpectedExpection(String ex, String storageString, String containerName, String imageString) : base(ex + ", expected: " + storageString + containerName + "/" + imageString) { }
    }
}
