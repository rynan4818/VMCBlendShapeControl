using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using VMCBlendShapeControl.Configuration;
using Zenject;

namespace VMCBlendShapeControl.Models
{
    public class VmcTimeExpression
    {
        public float SongTime;
        public VmcBlendShapeActionConfig Action;
    }

    public class VmcExpressionData : IInitializable
    {
        private readonly List<VmcTimeExpression> _timeScript = new List<VmcTimeExpression>();

        public string ScriptPath { get; private set; } = string.Empty;
        public int EventId { get; private set; }
        public VmcScriptSettings ScriptSettings { get; private set; } = new VmcScriptSettings();

        public void Initialize()
        {
            if (!File.Exists(PluginConfig.Instance.vmcExpressionScriptPath))
            {
                var dir = Path.GetDirectoryName(PluginConfig.DefaultScriptPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                PluginConfig.Instance.vmcExpressionScriptPath = PluginConfig.DefaultScriptPath;
            }
        }

        public bool LoadFromJson(string jsonString)
        {
            _timeScript.Clear();
            ScriptSettings = new VmcScriptSettings();

            VmcBlendShapeScriptJson parsed = null;
            var sep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            var sepCheck = sep == "." ? "," : ".";

            try
            {
                parsed = JsonConvert.DeserializeObject<VmcBlendShapeScriptJson>(jsonString);
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"JSON file syntax error. {ex.Message}");
            }

            if (parsed == null)
            {
                return false;
            }

            ScriptSettings = parsed.Settings ?? new VmcScriptSettings();

            if (parsed.JsonTimeScript != null)
            {
                foreach (var item in parsed.JsonTimeScript)
                {
                    if (item == null || item.Action == null || string.IsNullOrWhiteSpace(item.SongTime))
                    {
                        continue;
                    }

                    float songTime;
                    var normalized = item.SongTime.Contains(sepCheck) ? item.SongTime.Replace(sepCheck, sep) : item.SongTime;
                    if (!float.TryParse(normalized, out songTime))
                    {
                        if (!float.TryParse(item.SongTime, NumberStyles.Float, CultureInfo.InvariantCulture, out songTime))
                        {
                            continue;
                        }
                    }

                    _timeScript.Add(new VmcTimeExpression
                    {
                        SongTime = songTime,
                        Action = item.Action
                    });
                }
            }

            _timeScript.Sort((a, b) => a.SongTime.CompareTo(b.SongTime));
            return true;
        }

        public bool LoadVmcExpressionData(string path = null)
        {
            if (path == null)
            {
                path = PluginConfig.Instance.vmcExpressionScriptPath;
            }

            if (!File.Exists(path))
            {
                return false;
            }

            var jsonText = File.ReadAllText(path);
            if (!LoadFromJson(jsonText))
            {
                return false;
            }

            if (_timeScript.Count == 0)
            {
                Plugin.Log.Notice("No VMC BlendShape time data.");
                return false;
            }

            ScriptPath = path;
            Plugin.Log.Notice($"Found {_timeScript.Count} entries in: {path}");
            return true;
        }

        public void ResetEventID()
        {
            EventId = 0;
        }

        public VmcBlendShapeActionConfig UpdateEvent(float songTime)
        {
            if (EventId >= _timeScript.Count)
            {
                return null;
            }

            if (songTime != 0f && _timeScript[EventId].SongTime <= songTime)
            {
                return _timeScript[EventId++].Action?.Clone();
            }

            return null;
        }
    }
}
