using System.Linq;
using HashBang.CommandModule.Attributes;
using smIRCL.Extensions;

namespace HashBang.CommandModule.Internal
{
    public class FunModule
    {
        [Invocation("poke")]
        [Usage("<nick to poke>")]
        [Description("Poke someone")]
        public void Poke(CommandContext ctx)
        {
            if (ctx.Parameters.Count == 0)
            {
                ctx.NoticeReply("[ERROR] Usage: poke <nick to poke>");
                return;
            }

            if (ctx.Instance.Controller.Users.All(u => u.Nick.ToIrcLower() != ctx.Parameters[0].ToIrcLower()))
            {
                ctx.NoticeReply($"[ERROR] I do not share a channel or a recent private message with {ctx.Parameters[0]}");
                return;
            }

            ctx.Instance.Controller.SendPrivMsg(ctx.Parameters[0], $"{ctx.Message.SourceNick} has asked me to poke you!");
            ctx.Instance.Controller.SendPrivMsg(ctx.Parameters[0], "\u0001ACTION pokes\x01");
            ctx.NoticeReply($"I poked {ctx.Parameters[0]} for you!");
        }
    }
}
