using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HashBang.CommandModule;
using HashBang.CommandModule.Attributes;
using HashBang.CommandModule.Exceptions;
using smIRCL.Core;
using smIRCL.Extensions;
using smIRCL.ServerEntities;

namespace HashBang
{
    public class HashBangInstance
    {
        public IrcController Controller { get; internal set; }
        public string CommandPrefix { get; internal set; }
        public List<KeyValuePair<Type, object>> CommandModules = new List<KeyValuePair<Type, object>>();
        public List<KeyValuePair<string, MethodInfo>> RegisteredInvocations = new List<KeyValuePair<string, MethodInfo>>();
        public List<KeyValuePair<string, MethodInfo>> RegisteredCtcpInvocations = new List<KeyValuePair<string, MethodInfo>>();

        public HashBangInstance(IrcController controller, string commandPrefix)
        {
            Controller = controller ?? throw new ArgumentException($"Provided {nameof(controller)} must not be null");
            if (string.IsNullOrWhiteSpace(commandPrefix)) throw new ArgumentException($"Provided {nameof(commandPrefix)} is invalid");

            CommandPrefix = commandPrefix;

            Controller.PrivMsg += Controller_PrivMsg;
            Controller.Ctcp += Controller_Ctcp;
        }

        private void Controller_PrivMsg(IrcController controller, IrcMessage message)
        {
            if (message.Parameters[1].ToLower().StartsWith(CommandPrefix))
            {
                string replyTo = controller.IsValidChannelName(message.Parameters[0]) ? message.Parameters[0] : message.SourceNick;
                string[] splitMessage = message.Parameters[1].Split(' ');

                string command = splitMessage[0].ToLower();
                int prefixIndex = command.IndexOf(CommandPrefix, StringComparison.Ordinal);
                command = prefixIndex < 0 ? command : command.Remove(prefixIndex, CommandPrefix.Length);

                List<string> parameters = splitMessage.ToList();
                parameters.RemoveAt(0);

                KeyValuePair<string, MethodInfo> invocation = RegisteredInvocations.FirstOrDefault(inv => inv.Key.ToLower() == command);
                if (invocation.Key == null) return;

                KeyValuePair<Type, object> invokable = CommandModules.FirstOrDefault(inv => inv.Key == invocation.Value.DeclaringType);
                if (invokable.Key != null) invocation.Value.Invoke(invokable.Value, new object[] { new CommandContext(this, message, command, parameters, replyTo) });
            }
        }

        private void Controller_Ctcp(IrcController controller, IrcMessage message)
        {
            if (message.Parameters[1].ToLower().StartsWith("\x01"))
            {
                string replyTo = controller.IsValidChannelName(message.Parameters[0]) ? message.Parameters[0] : message.SourceNick;
                string[] splitMessage = message.Parameters[1].Trim('\x01').Split(' ');

                string command = splitMessage[0].ToLower();
                
                List<string> parameters = splitMessage.ToList();
                parameters.RemoveAt(0);

                if (command == "clientinfo")
                {
                    Controller_Ctcp_ClientInfo(new CommandContext(this, message, command, parameters, replyTo));
                    return;
                }

                KeyValuePair<string, MethodInfo> ctcpInvocation = RegisteredCtcpInvocations.FirstOrDefault(inv => inv.Key.ToLower() == command);
                if (ctcpInvocation.Key == null) return;

                KeyValuePair<Type, object> invokable = CommandModules.FirstOrDefault(inv => inv.Key == ctcpInvocation.Value.DeclaringType);
                if (invokable.Key != null) ctcpInvocation.Value.Invoke(invokable.Value, new object[] { new CommandContext(this, message, command, parameters, replyTo) });
            }
        }

        private void Controller_Ctcp_ClientInfo(CommandContext ctx)
        {

            string allInvocations = "CLIENTINFO";

            foreach (KeyValuePair<string, MethodInfo> invocation in RegisteredCtcpInvocations)
            {
                allInvocations += $" {invocation.Key.ToIrcUpper()}";
            }

            ctx.NoticeReply($"\u0001CLIENTINFO {allInvocations}\x01");
        }

        public T LoadModule<T>() where T : new()
        {
            if (CommandModules.Any(mod => mod.Key == typeof(T))) throw new AlreadyLoadedException();

            MethodInfo[] methodInfos = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Instance);

            foreach (MethodInfo info in methodInfos)
            {
                ParameterInfo[] parameters = info.GetParameters();
                if (parameters.Length != 1 || parameters[0].ParameterType != typeof(CommandContext)) continue;

                IEnumerable<InvocationAttribute> invocations = (IEnumerable<InvocationAttribute>) info.GetCustomAttributes(typeof(InvocationAttribute));

                foreach (InvocationAttribute invocation in invocations)
                {
                    if (RegisteredInvocations.All(inv => !string.Equals(inv.Key, invocation.Invocation, StringComparison.CurrentCultureIgnoreCase))) RegisteredInvocations.Add(new KeyValuePair<string, MethodInfo>(invocation.Invocation, info));
                }

                IEnumerable<CtcpInvocationAttribute> CtcpHandlers = (IEnumerable<CtcpInvocationAttribute>) info.GetCustomAttributes(typeof(CtcpInvocationAttribute));

                foreach (CtcpInvocationAttribute ctcpInvocation in CtcpHandlers)
                {
                    if (RegisteredCtcpInvocations.All(inv => !string.Equals(inv.Key, ctcpInvocation.Invocation, StringComparison.CurrentCultureIgnoreCase))) RegisteredCtcpInvocations.Add(new KeyValuePair<string, MethodInfo>(ctcpInvocation.Invocation, info));
                }
            }

            T module = new T();

            CommandModules.Add(new KeyValuePair<Type, object>(typeof(T), module));

            return module;
        }
    }
}
