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
        public float defaultFacialExpressionTransition { get; set; } = 0.1f;
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

    public class NalulunaAvatarEvent
    {
        public float _time { get; set; }
        public float _duration { get; set; }
        public string _key { get; set; } = string.Empty;
        public string _value { get; set; } = string.Empty;
    }

    public class NalulunaAvatarEventSettings
    {
        public float defaultFacialExpressionTransitionSpeed { get; set; } = 10f;
    }

    public class NalulunaAvatarsEventsJson
    {
        public string _version { get; set; } = "0.0.1";
        public List<NalulunaAvatarEvent> _events { get; set; } = new List<NalulunaAvatarEvent>();
        public NalulunaAvatarEventSettings _settings { get; set; } = new NalulunaAvatarEventSettings();
    }
}
