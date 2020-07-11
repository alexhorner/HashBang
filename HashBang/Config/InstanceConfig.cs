using System.Collections.Generic;
using Newtonsoft.Json;

namespace HashBang.Config
{
    public class InstanceConfig
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("autostart")]
        public bool AutoStart { get; set; }

        [JsonProperty("commandprefix")]
        public string CommandPrefix { get; set; }

        [JsonProperty("host")]
        public string Host { get; set; }

        [JsonProperty("port")]
        public int Port { get; set; }

        [JsonProperty("ssl")]
        public bool UseSsl { get; set; }

        [JsonProperty("nick")]
        public string Nick { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("realname")]
        public string RealName { get; set; }

        [JsonProperty("channels")]
        public List<string> Channels { get; set; }

        [JsonProperty("sasl")]
        public SaslConfig Sasl { get; set; }
    }
}
