using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HashBang.CommandModule.Attributes;

namespace HashBang.CommandModule.Internal
{
    [Name("Help")]
    public class HelpMenuModule
    {
        [Invocation("help")]
        [Invocation("?")]
        [Description("Shows the help menu")]
        public void Help(CommandContext ctx)
        {
            List<KeyValuePair<string, List<KeyValuePair<string, string>>>> modulesAndCommandsWithDescriptions = new List<KeyValuePair<string, List<KeyValuePair<string, string>>>>();

            foreach (KeyValuePair<Type, object> commandModule in ctx.Instance.CommandModules)
            {
                if (commandModule.Key.GetCustomAttributes(typeof(HiddenAttribute), false).Length > 0) continue;

                NameAttribute[] nameAttributes = (NameAttribute[]) commandModule.Key.GetCustomAttributes(typeof(NameAttribute), false);
                string moduleName = nameAttributes.Length > 0 ? nameAttributes[0].Name : commandModule.Key.Name;

                List<KeyValuePair<string, string>> commandsAndDescriptions = new List<KeyValuePair<string, string>>();

                MethodInfo[] methodInfos = commandModule.Key.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                foreach (MethodInfo info in methodInfos)
                {
                    ParameterInfo[] parameters = info.GetParameters();
                    if (parameters.Length != 1 || parameters[0].ParameterType != typeof(CommandContext)) continue;
                    if (info.GetCustomAttributes(typeof(HiddenAttribute), false).Length > 0) continue;

                    IEnumerable<InvocationAttribute> invocations = (IEnumerable<InvocationAttribute>) info.GetCustomAttributes(typeof(InvocationAttribute));
                    if (!invocations.Any()) continue;

                    IEnumerable<DescriptionAttribute> descriptions = (IEnumerable<DescriptionAttribute>)info.GetCustomAttributes(typeof(DescriptionAttribute));

                    commandsAndDescriptions.Add(new KeyValuePair<string, string>(invocations.First().Invocation, descriptions.Any() ? descriptions.First().Description : null));
                }

                modulesAndCommandsWithDescriptions.Add(new KeyValuePair<string, List<KeyValuePair<string, string>>>(moduleName, commandsAndDescriptions));
            }

            ctx.NoticeReply("==== HELP MENU ====");
            ctx.NoticeReply("===================");

            foreach (KeyValuePair<string, List<KeyValuePair<string, string>>> module in modulesAndCommandsWithDescriptions)
            {
                ctx.NoticeReply($"> {module.Key} <");

                foreach (KeyValuePair<string, string> command in module.Value)
                {
                    ctx.NoticeReply($"- {command.Key}{(command.Value != null ? $" -> {command.Value}" : "")}");
                }
            }

            ctx.NoticeReply("===================");
        }

        [Invocation("usage")]
        [Usage("<command>")]
        [Description("Explains the usage of a command")]
        public void Usage(CommandContext ctx)
        {
            if (ctx.Parameters.Count == 0)
            {
                ctx.NoticeReply("[ERROR] Usage: usage <command>");
                return;
            }

            KeyValuePair<string, MethodInfo> invocation = ctx.Instance.RegisteredInvocations.FirstOrDefault(inv => String.Equals(inv.Key, ctx.Parameters[0], StringComparison.CurrentCultureIgnoreCase));

            if (invocation.Key != null)
            {
                IEnumerable<UsageAttribute> usages = (IEnumerable<UsageAttribute>) invocation.Value.GetCustomAttributes(typeof(UsageAttribute));

                ctx.NoticeReply(usages.Any() ? $"Usage: {invocation.Key} {usages.First().Usage}" : $"[ERROR] Command '{invocation.Key}' has no defined usage");

                return;
            }

            ctx.NoticeReply($"[ERROR] Unknown command '{ctx.Parameters[0]}'");
        }
    }
}
