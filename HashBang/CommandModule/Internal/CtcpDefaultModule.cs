using HashBang.CommandModule.Attributes;

namespace HashBang.CommandModule.Internal
{
    [Hidden]
    public class CtcpDefaultModule
    {
        [CtcpInvocation("ping")]
        public void Ping(CommandContext ctx)
        {
            string allParameters = null;

            foreach (string parameter in ctx.Parameters)
            {
                if (allParameters != null)
                {
                    allParameters += $" {parameter}";
                }
                else
                {
                    allParameters = parameter;
                }
            }

            ctx.NoticeReply($"\x01PING {allParameters}\x01");
        }
    }
}
