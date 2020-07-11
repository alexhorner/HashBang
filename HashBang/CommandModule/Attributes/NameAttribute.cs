using System;

namespace HashBang.CommandModule.Attributes
{
    /// <summary>
    /// Indicates the friendly name for a command module or one of its commands
    /// </summary>
    public class NameAttribute : Attribute
    {
        public string Name { get; internal set; }

        public NameAttribute(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException($"The parameter '{nameof(name)}' is invalid");

            Name = name;
        }
    }
}
