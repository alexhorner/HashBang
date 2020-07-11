using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace HashBang.Config
{
    public class HashBangConfig
    {
        [JsonProperty("autostart")]
        public bool AutoStart { get; set; }

        [JsonProperty("instances")]
        public List<InstanceConfig> Instances { get; set; }

        public static HashBangConfig LoadFromJsonFile(string path)
        {
            return JsonConvert.DeserializeObject<HashBangConfig>(File.ReadAllText(path));
        }
    }
}
