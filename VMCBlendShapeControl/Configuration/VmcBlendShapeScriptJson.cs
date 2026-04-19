using System.Collections.Generic;
using Newtonsoft.Json;

namespace VMCBlendShapeControl.Configuration
{
    [JsonObject("TimeScript")]
    public class VmcJsonTimeScript
    {
        public string SongTime { get; set; }
        public VmcBlendShapeActionConfig Action { get; set; }
    }

    public class VmcScriptSettings
    {
        public float defaultFacialExpressionTransitionSpeed { get; set; } = 10f;
        public List<string> blendShapesNoBlinkUser { get; set; } = new List<string>();
        public List<string> noDefaultBlendShapeChangeKeys { get; set; } = new List<string>();
    }

    public class VmcBlendShapeScriptJson
    {
        [JsonProperty("Version")]
        public string Version { get; set; } = "1.0.0";

        [JsonProperty("TimeScript")]
        public VmcJsonTimeScript[] JsonTimeScript { get; set; }

        [JsonProperty("Settings")]
        public VmcScriptSettings Settings { get; set; } = new VmcScriptSettings();
    }
}
