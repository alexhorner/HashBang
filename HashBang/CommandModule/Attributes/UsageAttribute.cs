using System;

namespace HashBang.CommandModule.Attributes
{
    /// <summary>
    /// Indicates the usage of a command module command
    /// </summary>
    public class UsageAttribute : Attribute
    {
        public string Usage { get; internal set; }

        public UsageAttribute(string usage)
        {
            if (string.IsNullOrWhiteSpace(usage)) throw new ArgumentException($"The parameter '{nameof(usage)}' is invalid");

            Usage = usage;
        }
    }
}
