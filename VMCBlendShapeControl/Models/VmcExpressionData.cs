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

            List<VmcTimeExpression> parsedTimeScript;
            VmcScriptSettings parsedSettings;

            if (TryLoadVmcBlendShapeScript(jsonString, out parsedTimeScript, out parsedSettings) ||
                TryLoadNalulunaEventsScript(jsonString, out parsedTimeScript, out parsedSettings))
            {
                _timeScript.AddRange(parsedTimeScript);
                ScriptSettings = parsedSettings ?? new VmcScriptSettings();
                return true;
            }

            return false;
        }

        private bool TryLoadVmcBlendShapeScript(string jsonString, out List<VmcTimeExpression> timeScript, out VmcScriptSettings scriptSettings)
        {
            timeScript = new List<VmcTimeExpression>();
            scriptSettings = new VmcScriptSettings();

            VmcBlendShapeScriptJson parsed = null;

            try
            {
                parsed = JsonConvert.DeserializeObject<VmcBlendShapeScriptJson>(jsonString);
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"JSON file syntax error. {ex.Message}");
                return false;
            }

            if (parsed == null)
            {
                return false;
            }

            scriptSettings = parsed.Settings ?? new VmcScriptSettings();

            if (parsed.JsonTimeScript == null)
            {
                return false;
            }

            foreach (var item in parsed.JsonTimeScript)
            {
                if (item == null || item.Action == null || string.IsNullOrWhiteSpace(item.SongTime))
                {
                    continue;
                }

                float songTime;
                if (!TryParseFloat(item.SongTime, out songTime))
                {
                    continue;
                }

                timeScript.Add(new VmcTimeExpression
                {
                    SongTime = Math.Max(0f, songTime),
                    Action = item.Action
                });
            }

            timeScript.Sort((a, b) => a.SongTime.CompareTo(b.SongTime));
            return timeScript.Count > 0;
        }

        private bool TryLoadNalulunaEventsScript(string jsonString, out List<VmcTimeExpression> timeScript, out VmcScriptSettings scriptSettings)
        {
            timeScript = new List<VmcTimeExpression>();
            scriptSettings = new VmcScriptSettings();

            NalulunaAvatarsEventsJson parsed = null;
            try
            {
                parsed = JsonConvert.DeserializeObject<NalulunaAvatarsEventsJson>(jsonString);
            }
            catch
            {
                return false;
            }

            if (parsed == null || parsed._events == null)
            {
                return false;
            }

            scriptSettings = ConvertNalulunaSettings(parsed._settings);

            foreach (var item in parsed._events.OrderBy(x => x == null ? float.MaxValue : x._time))
            {
                if (item == null || string.IsNullOrWhiteSpace(item._key))
                {
                    continue;
                }

                if (item._key.Equals("BlendShape", StringComparison.OrdinalIgnoreCase))
                {
                    string blendShape;
                    float value;
                    if (!TryParseBlendShapeValue(item._value, out blendShape, out value))
                    {
                        continue;
                    }

                    timeScript.Add(new VmcTimeExpression
                    {
                        SongTime = Math.Max(0f, item._time),
                        Action = new VmcBlendShapeActionConfig
                        {
                            BlendShape = blendShape,
                            Value = value,
                            Duration = Math.Max(0f, item._duration),
                            Transition = 0f
                        }
                    });
                    continue;
                }
            }

            timeScript.Sort((a, b) => a.SongTime.CompareTo(b.SongTime));
            return timeScript.Count > 0;
        }

        private static VmcScriptSettings ConvertNalulunaSettings(NalulunaAvatarEventSettings eventSettings)
        {
            var settings = new VmcScriptSettings();
            if (eventSettings == null)
            {
                return settings;
            }

            if (eventSettings.defaultFacialExpressionTransitionSpeed > 0f)
            {
                settings.defaultFacialExpressionTransition = 1.66f / eventSettings.defaultFacialExpressionTransitionSpeed;
            }

            return settings;
        }

        private static bool TryParseBlendShapeValue(string rawValue, out string blendShape, out float value)
        {
            blendShape = string.Empty;
            value = 1f;

            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return false;
            }

            var setData = rawValue.Split(new[] { ',' }, 2, StringSplitOptions.None);
            blendShape = setData[0].Trim();

            if (string.IsNullOrWhiteSpace(blendShape))
            {
                return false;
            }

            if (setData.Length >= 2)
            {
                float parsedValue;
                if (TryParseFloat(setData[1], out parsedValue))
                {
                    value = Math.Max(0f, Math.Min(1f, parsedValue));
                }
            }

            return true;
        }

        private static bool TryParseFloat(string valueText, out float value)
        {
            value = 0f;
            if (string.IsNullOrWhiteSpace(valueText))
            {
                return false;
            }

            var text = valueText.Trim();
            if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }

            var sep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            var altSep = sep == "." ? "," : ".";
            var normalized = text.Contains(altSep) ? text.Replace(altSep, sep) : text;
            return float.TryParse(normalized, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
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
