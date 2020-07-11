using System;
using System.Collections.Generic;
using System.Linq;
using HashBang.CommandModule.Internal;
using HashBang.Config;
using HashBang.InstanceManagement;
using HashBang.Utility;
using smIRCL.Config;
using smIRCL.Core;
using smIRCL.Enums;
using smIRCL.ServerEntities;

namespace HashBang
{
    class Program
    {
        private static readonly InstanceController InstanceController = new InstanceController();
        private static readonly List<string> ExpectingTermination = new List<string>();
        private static HashBangConfig _config;
        private static bool _running = true;

        #region Main Execution

        static void Main(string[] args)
        {
            Console.Title = "HashBang | #!";
            Console.Clear();

            #region Fancy Text

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(@"    __  __           __    ____                         __ __  __");
            Console.WriteLine(@"   / / / /___ ______/ /_  / __ )____ _____  ____ _   __/ // /_/ /");
            Console.WriteLine(@"  / /_/ / __ `/ ___/ __ \/ __  / __ `/ __ \/ __ `/  /_  _  __/ / ");
            Console.WriteLine(@" / __  / /_/ (__  ) / / / /_/ / /_/ / / / / /_/ /  /_  _  __/_/  ");
            Console.WriteLine(@"/_/ /_/\__,_/____/_/ /_/_____/\__,_/_/ /_/\__, /    /_//_/ (_)   ");
            Console.WriteLine(@"                                         /____/                  ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();

            #endregion

            _config = HashBangConfig.LoadFromJsonFile("./config.json");

            InstanceController.InstanceDied += InstanceController_InstanceDied;

            if (_config.AutoStart)
            {
                ConOut.Info("AutoStart enabled... Starting instances");
                RunStartSequence(true);
                ConOut.Ok("Instances have been started");
            }

            while (_running)
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write("#!> ");
                Console.ForegroundColor = ConsoleColor.White;
                string command = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(command)) ProcessCommand(command);
            }
        }

        private static void InstanceController_InstanceDied(string instanceName, HashBangInstance instance)
        {
            string expectingTermination = ExpectingTermination.FirstOrDefault();

            if (expectingTermination != null)
            {
                ExpectingTermination.RemoveAll(exp => String.Equals(exp, instanceName, StringComparison.CurrentCultureIgnoreCase));
                return;
            }

            ConOut.Error($"Instance '{instanceName}' has died");

            try
            {
                instance.Controller.Quit();
            }
            catch
            {
                //Ignored
            }

            InstanceController.RemoveInstance(instanceName);

            ConOut.Ok($"Instance '{instanceName}' cleaned up after death");
        }

        private static void StartNewInstance(InstanceConfig config)
        {
            IrcConnector connector = new IrcConnector(new IrcConfig
            {
                ServerHostname = config.Host,
                ServerPort = config.Port,
                Nick = config.Nick,
                UserName = config.Username,
                RealName = config.RealName,
                UseSsl = config.UseSsl,
                AuthMode = config.Sasl.Enabled ? AuthMode.SASL : AuthMode.None,
                AuthUsername = config.Sasl.Username,
                AuthPassword = config.Sasl.Password,
                AutoJoinChannels = config.Channels
            });

            connector.Config.IsValid(true);
            HashBangInstance instance = new HashBangInstance(new IrcController(connector), config.CommandPrefix);

            #region Load Modules

            instance.LoadModule<CtcpDefaultModule>();
            instance.LoadModule<HelpMenuModule>();
            instance.LoadModule<FunModule>();

            #endregion

            InstanceController.AddInstance(config.Name, instance);
            connector.Connect();
        }

        private static void RunStartSequence(bool respectAutoStart = false)
        {
            foreach (InstanceConfig instanceConfig in _config.Instances)
            {
                if (InstanceController.ContainsInstance(instanceConfig.Name)) continue;
                if (respectAutoStart && !instanceConfig.AutoStart) continue;

                try
                {
                    StartNewInstance(instanceConfig);
                }
                catch (Exception e)
                {
                    ConOut.Error($"Instance start error for '{instanceConfig.Name}': {e}");
                    continue;
                }

                ConOut.Ok($"Instance '{instanceConfig.Name}' started");
            }
        }

        private static void ProcessCommand(string command)
        {
            Console.WriteLine();

            List<string> parts = command.Split(' ').ToList();

            switch (parts[0].ToLower())
            {
                case "help":
                    HelpCommand();
                    break;

                case "restart":
                    RestartCommand(parts);
                    break;

                case "stop":
                    StopCommand(parts);
                    break;

                case "start":
                    StartCommand(parts);
                    break;

                case "list":
                    ListCommand(parts);
                    break;

                case "attach":
                    AttachCommand(parts);
                    break;

                case "quit":
                    QuitCommand();
                    break;

                default:
                    ConOut.Warning("Unknown Command");
                    break;
            }

            Console.WriteLine();
        }

        #endregion

        #region Commands

        private static void HelpCommand()
        {
            Console.WriteLine("=====================");
            Console.WriteLine("=== HashBang HELP ===");
            Console.WriteLine("=====================");
            Console.WriteLine();
            Console.WriteLine("<parameter> - Required parameter");
            Console.WriteLine("[parameter] - Optional parameter");
            Console.WriteLine();
            Console.WriteLine("<<parameter>> - Required parameter with strict options shown");
            Console.WriteLine("[[parameter]] - Optional parameter with strict options shown");
            Console.WriteLine();
            Console.WriteLine("=====================");
            Console.WriteLine();
            Console.WriteLine("help - This command");
            Console.WriteLine("restart [instance] - Restart one or all instances");
            Console.WriteLine("stop [instance] - Stop one or all instances");
            Console.WriteLine("start [instance] - Start one or all instances");
            Console.WriteLine("list <<all/started/stopped>> - Show a list of instances in the specified state");
            Console.WriteLine("attach <instance> - Attach to and take full control of the specified instance");
            Console.WriteLine("quit - Stop all instances and quit");
        }

        private static void RestartCommand(List<string> parts)
        {
            if (parts.Count > 1)
            {
                if (!InstanceController.ContainsInstance(parts[1])) ConOut.Error($"The requested instance '{parts[1]}' could not be found");

                IrcController instanceIrcController = InstanceController.GetInstance(parts[1]).Controller;

                try
                {
                    instanceIrcController.Quit();
                }
                catch
                {
                    //Ignored
                }

                InstanceController.RemoveInstance(parts[1]);

                ConOut.Ok($"Instance '{parts[1]}' stopped");

                RunStartSequence();

                return;
            }

            ConOut.Warning("Are you sure you want to restart all instances? [y/N]");
            ConsoleKeyInfo option = Console.ReadKey();
            if (option.KeyChar != 'y' && option.KeyChar != 'Y') return;
            Console.WriteLine();

            ConOut.Info("All instances are going down for restart...");

            List<KeyValuePair<string, HashBangInstance>> instances = InstanceController.GetAllInstances();

            foreach (KeyValuePair<string, HashBangInstance> instance in instances)
            {
                try
                {
                    instance.Value.Controller.Quit();
                }
                catch
                {
                    //Ignored
                }

                InstanceController.RemoveInstance(instance.Key);

                ConOut.Ok($"Instance '{instance.Key}' stopped");
            }

            RunStartSequence();
        }

        private static void StopCommand(List<string> parts)
        {
            if (parts.Count > 1)
            {
                if (!InstanceController.ContainsInstance(parts[1]))
                {
                    ConOut.Error($"The requested instance '{parts[1]}' could not be found");
                    return;
                }

                ExpectingTermination.Add(parts[1]);

                IrcController instanceIrcController = InstanceController.GetInstance(parts[1]).Controller;

                try
                {
                    instanceIrcController.Quit();
                }
                catch
                {
                    //Ignored
                }

                InstanceController.RemoveInstance(parts[1]);

                ConOut.Ok($"Instance '{parts[1]}' stopped");

                return;
            }

            ConOut.Warning("Are you sure you want to stop all instances? [y/N]");
            ConsoleKeyInfo option = Console.ReadKey();
            if (option.KeyChar != 'y' && option.KeyChar != 'Y') return;
            Console.WriteLine();

            ConOut.Info("All instances are going down for stop...");

            List<KeyValuePair<string, HashBangInstance>> instances = InstanceController.GetAllInstances();

            foreach (KeyValuePair<string, HashBangInstance> instance in instances)
            {
                try
                {
                    instance.Value.Controller.Quit();
                }
                catch
                {
                    //Ignored
                }

                InstanceController.RemoveInstance(instance.Key);

                ConOut.Ok($"Instance '{instance.Key}' stopped");
            }
        }

        private static void StartCommand(List<string> parts)
        {
            if (parts.Count > 1)
            {
                if (InstanceController.ContainsInstance(parts[1]))
                {
                    ConOut.Error($"The requested instance '{parts[1]}' is already running");
                    return;
                }

                if (_config.Instances.All(insconf => !string.Equals(insconf.Name, parts[1], StringComparison.CurrentCultureIgnoreCase)))
                {
                    ConOut.Error($"The requested instance '{parts[1]}' does not exist");
                    return;
                }

                InstanceConfig instanceConfig = _config.Instances.FirstOrDefault(insconf => string.Equals(insconf.Name, parts[1], StringComparison.CurrentCultureIgnoreCase));

                try
                {
                    StartNewInstance(instanceConfig);
                }
                catch (Exception e)
                {
                    ConOut.Error($"Instance start error for '{instanceConfig.Name}': {e}");
                    return;
                }

                ConOut.Ok($"Instance '{parts[1]}' started");

                return;
            }

            ConOut.Warning("Are you sure you want to start all instances? [y/N]");
            ConsoleKeyInfo option = Console.ReadKey();
            if (option.KeyChar != 'y' && option.KeyChar != 'Y') return;
            Console.WriteLine();

            RunStartSequence();
        }

        private static void ListCommand(List<string> parts)
        {
            if (parts.Count < 2)
            {
                ConOut.Error("No list type was specified");
                return;
            }

            string mode = parts[1].ToLower();

            switch (mode)
            {
                case "all":
                    foreach (InstanceConfig instance in _config.Instances)
                    {
                        Console.WriteLine($"> {instance.Name} - {(InstanceController.ContainsInstance(instance.Name) ? "Started" : "Stopped")}");
                    }
                    break;

                case "started":
                    foreach (InstanceConfig instance in _config.Instances)
                    {
                        if (InstanceController.ContainsInstance(instance.Name))  Console.WriteLine($"> {instance.Name}");
                    }
                    break;

                case "stopped":
                    foreach (InstanceConfig instance in _config.Instances)
                    {
                        if (!InstanceController.ContainsInstance(instance.Name)) Console.WriteLine($"> {instance.Name}");
                    }
                    break;

                default:
                    ConOut.Error("Invalid list type was specified");
                    break;
            }
        }

        private static void AttachCommand(List<string> parts)
        {
            if (parts.Count < 2)
            {
                ConOut.Error("No instance was specified");
                return;
            }

            if (!InstanceController.ContainsInstance(parts[1]))
            {
                ConOut.Error($"The requested instance '{parts[1]}' could not be found");
                return;
            }


            if (!InstanceController.ContainsInstance(parts[1]))
            {
                ConOut.Error($"The requested instance '{parts[1]}' could not be found");
                return;
            }

            ConOut.Warning($"Now attaching to instance '{parts[1]}'. Type 'detach' to detach again");

            IrcController instanceIrcController = InstanceController.GetInstance(parts[1]).Controller;

            instanceIrcController.Connector.MessageReceived += ConnectorMessageReceived;
            instanceIrcController.Connector.MessageTransmitted += ConnectorOnMessageTransmitted;

            while (!instanceIrcController.Connector.IsDisposed)
            {
                string msg = Console.ReadLine();

                if (msg != null && msg.ToLower() == "detach")
                {
                    break;
                }

                try
                {
                    instanceIrcController.Connector.Transmit(msg);
                }
                catch
                {
                    //ignore
                }
            }

            instanceIrcController.Connector.MessageReceived -= ConnectorMessageReceived;
            instanceIrcController.Connector.MessageTransmitted -= ConnectorOnMessageTransmitted;
        }

        private static void QuitCommand()
        {
            ConOut.Warning("Are you sure you want to stop all instances and quit? [y/N]");
            ConsoleKeyInfo option = Console.ReadKey();
            if (option.KeyChar != 'y' && option.KeyChar != 'Y') return;
            Console.WriteLine();


            ConOut.Info("All instances are going down for quit...");

            List<KeyValuePair<string, HashBangInstance>> instances = InstanceController.GetAllInstances();

            foreach (KeyValuePair<string, HashBangInstance> instance in instances)
            {
                ExpectingTermination.Add(instance.Key);

                try
                {
                    instance.Value.Controller.Quit();
                }
                catch
                {
                    //Ignored
                }

                InstanceController.RemoveInstance(instance.Key);

                ConOut.Ok($"Instance '{instance.Key}' stopped");
            }

            Environment.Exit(0);
        }

        #endregion

        #region Instance Attach Outputs

        private static void ConnectorOnMessageTransmitted(string rawMessage)
        {
            Console.WriteLine("<<<    " + rawMessage);
        }

        private static void ConnectorMessageReceived(string rawMessage, IrcMessage message)
        {
            Console.WriteLine(">>>    " + rawMessage);
        }

        #endregion
    }
}
