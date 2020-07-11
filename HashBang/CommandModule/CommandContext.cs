using System;
using System.Collections.Generic;
using smIRCL.ServerEntities;

namespace HashBang.CommandModule
{
    /// <summary>
    /// The context of a command invocation
    /// </summary>
    public class CommandContext
    {
        public HashBangInstance Instance { get; internal set; }
        public IrcMessage Message { get; internal set; }
        public string Invocation { get; internal set; }
        public List<string> Parameters { get; internal set; }
        public string ReplyTo { get; internal set; }

        public CommandContext(HashBangInstance instance, IrcMessage message, string invocation, List<string> parameters, string replyTo)
        {
            if (string.IsNullOrWhiteSpace(invocation)) throw new ArgumentException($"Parameter {nameof(invocation)} has an invalid value");
            if (string.IsNullOrWhiteSpace(replyTo)) throw new ArgumentException($"Parameter {nameof(replyTo)} has an invalid value");

            Instance = instance ?? throw new ArgumentException($"Parameter {nameof(instance)} must not be null");
            Message = message ?? throw new ArgumentException($"Parameter {nameof(message)} must not be null");
            Parameters = parameters ?? throw new ArgumentException($"Parameter {nameof(parameters)} must not be null");
            Invocation = invocation;
            ReplyTo = replyTo;
        }

        public void Reply(string message)
        {
            Instance.Controller.SendPrivMsg(ReplyTo, message);
        }

        public void NoticeReply(string message)
        {
            Instance.Controller.SendNotice(ReplyTo, message);
        }
    }
}
