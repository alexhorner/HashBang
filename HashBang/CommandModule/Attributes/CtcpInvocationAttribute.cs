using System;
using smIRCL.Extensions;

namespace HashBang.CommandModule.Attributes
{
    /// <summary>
    /// Indicates what CTCP command would be used to invoke a command module method
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class CtcpInvocationAttribute : Attribute
    {
        public string Invocation { get; internal set; }

        public CtcpInvocationAttribute(string invocation)
        {
            if (invocation.ToIrcLower() == "clientinfo") throw new ArgumentException($"The parameter {nameof(invocation)} must not be 'clientinfo' as this is internally controlled");
            Invocation = invocation.ToIrcLower();
        }
    }
}
