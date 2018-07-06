using System;

namespace Back.Exceptions
{
    public class EmptyContainerExpection : EmptyExpectedExpection
    {
        public EmptyContainerExpection(String containerName) : base("Container and Item Name cannot be empty", containerName) { }
    }
}
