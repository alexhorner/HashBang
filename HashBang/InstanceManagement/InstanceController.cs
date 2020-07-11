using System;
using System.Collections.Generic;
using System.Linq;
using HashBang.InstanceManagement.Exceptions;
using smIRCL.Core;

namespace HashBang.InstanceManagement
{
    /// <summary>
    /// Handles multiple instances of the bot across servers
    /// </summary>
    public class InstanceController
    {
        private readonly Dictionary<string, HashBangInstance> _instances = new Dictionary<string, HashBangInstance>();

        /// <summary>
        /// Fired when an instance dies
        /// </summary>
        public event InstanceDeathHandler InstanceDied;
        /// <summary>
        /// The handler for instance death events
        /// </summary>
        /// <param name="instanceName">The name of the instance which died</param>
        /// <param name="controller">The instance which died</param>
        public delegate void InstanceDeathHandler(string instanceName, HashBangInstance controller);

        private void OnInstanceDeath(IrcController controller)
        {
            KeyValuePair<string, HashBangInstance> deadInstance = _instances.FirstOrDefault(instance => instance.Value.Controller == controller);

            if (deadInstance.Key == null || deadInstance.Value.Controller.Connector.IsConnected) return;

            InstanceDied?.Invoke(deadInstance.Key, deadInstance.Value);
        }

        /// <summary>
        /// Retrieves a named instance
        /// </summary>
        /// <param name="instanceName">Case insensitive name of an instance</param>
        /// <returns>An instance, if found</returns>
        public HashBangInstance GetInstance(string instanceName)
        {
            return _instances.FirstOrDefault(instance => string.Equals(instance.Key, instanceName, StringComparison.CurrentCultureIgnoreCase)).Value;
        }

        /// <summary>
        /// Retrieves all instances
        /// </summary>
        /// <returns>A list of instances</returns>
        public List<KeyValuePair<string, HashBangInstance>> GetAllInstances()
        {
            return _instances.ToList();
        }

        /// <summary>
        /// Adds a named instance
        /// </summary>
        /// <param name="instanceName">Case insensitive name of an instance</param>
        /// <param name="controller">The instance controller</param>
        public void AddInstance(string instanceName, HashBangInstance controller)
        {
            if (_instances.Any(instance => string.Equals(instance.Key, instanceName, StringComparison.CurrentCultureIgnoreCase))) throw new InstanceExistsException($"An instance with the specified {nameof(instanceName)} already exists");

            controller.Controller.Disconnected += OnInstanceDeath;
            _instances.Add(instanceName, controller);
        }

        /// <summary>
        /// Removes a named instance
        /// </summary>
        /// <param name="instanceName">Case insensitive name of an instance</param>
        public void RemoveInstance(string instanceName)
        {
            KeyValuePair<string, HashBangInstance> instanceSelected = _instances.FirstOrDefault(instance => string.Equals(instance.Key, instanceName, StringComparison.CurrentCultureIgnoreCase));

            if (instanceSelected.Key == null || instanceSelected.Value == null) throw new InvalidOperationException($"The specified {nameof(instanceName)} could not be found");

            instanceSelected.Value.Controller.Disconnected -= OnInstanceDeath;

            _instances.Remove(instanceSelected.Key);
        }

        /// <summary>
        /// Removes a specific instance
        /// </summary>
        /// <param name="controller">The instance controller</param>
        public void RemoveInstance(HashBangInstance controller)
        {
            KeyValuePair<string, HashBangInstance> instanceSelected = _instances.FirstOrDefault(instance => instance.Value == controller);

            if (instanceSelected.Key == null || instanceSelected.Value == null) throw new InvalidOperationException($"The specified {nameof(controller)} could not be found");

            instanceSelected.Value.Controller.Disconnected -= OnInstanceDeath;

            _instances.Remove(instanceSelected.Key);
        }

        /// <summary>
        /// Checks if a named instance exists
        /// </summary>
        /// <param name="instanceName">Case insensitive name of an instance</param>
        /// <returns>Whether the instance exists</returns>
        public bool ContainsInstance(string instanceName)
        {
            return _instances.Any(instance => string.Equals(instance.Key, instanceName, StringComparison.CurrentCultureIgnoreCase));
        }

        /// <summary>
        /// Retrieves instances which were running but have now died
        /// </summary>
        /// <returns>Instances which have died</returns>
        public List<KeyValuePair<string, HashBangInstance>> GetDeadInstances()
        {
            return _instances.Where(instance => !instance.Value.Controller.Connector.IsConnected && instance.Value.Controller.Connector.IsDisposed).ToList();
        }

        /// <summary>
        /// Retrieves instances which have not yet been started
        /// </summary>
        /// <returns>Instances waiting to be started</returns>
        public List<KeyValuePair<string, HashBangInstance>> GetUnusedInstances()
        {
            return _instances.Where(instance => !instance.Value.Controller.Connector.IsConnected && !instance.Value.Controller.Connector.IsDisposed).ToList();
        }
    }
}
