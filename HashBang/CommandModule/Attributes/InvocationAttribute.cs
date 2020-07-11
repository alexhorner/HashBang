using System;
using smIRCL.Extensions;

namespace HashBang.CommandModule.Attributes
{
    /// <summary>
    /// Indicates what command would be used to invoke a command module method
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class InvocationAttribute : Attribute
    {
        public string Invocation { get; internal set; }

        public InvocationAttribute(string invocation)
        {
            Invocation = invocation.ToIrcLower();
        }
    }
}
