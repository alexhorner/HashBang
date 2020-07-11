using System;

namespace HashBang.InstanceManagement.Exceptions
{
    public class InstanceExistsException : Exception
    {
        public InstanceExistsException(string message, Exception innerException = null) : base(message, innerException)
        {
        }
    }
}
