using System;

namespace HashBang.CommandModule.Attributes
{
    /// <summary>
    /// Indicates the description of the command module or one of its commands
    /// </summary>
    public class DescriptionAttribute : Attribute
    {
        public string Description { get; internal set; }

        public DescriptionAttribute(string description)
        {
            if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException($"The parameter '{nameof(description)}' is invalid");

            Description = description;
        }
    }
}
