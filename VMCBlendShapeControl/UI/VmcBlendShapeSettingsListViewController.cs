using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using IPA.Utilities;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using VMCBlendShapeControl.Configuration;
using VMCBlendShapeControl.Models;
using Zenject;

namespace VMCBlendShapeControl.UI
{
    [HotReload]
    internal class VmcBlendShapeSettingsListViewController : BSMLAutomaticViewController
    {
        private static readonly string[] DefaultBlendShapeNames =
        {
            "A", "I", "U", "E", "O",
            "Joy", "Angry", "Sorrow", "Fun",
            "Blink", "Blink_L", "Blink_R", "Neutral"
        };

        private static readonly string EventActionSettingsDirectory = Path.Combine(IPA.Utilities.UnityGame.UserDataPath, "VMCBlendShapeControl", "EventActionSettings");
        private const string NoEventActionPresetOption = "(No Preset)";

        private VmcBlendShapeCatalog _catalog;
        private VmcOscReceiver _oscReceiver;
        private readonly List<object> _eventTargetOptions;
        private readonly List<object> _eventBlendShapeOptions = new List<object>();
        private readonly List<object> _eventActionPresetOptions = new List<object>();
        private string _selectedEventTargetKey = EventTargetKeys.GameStart;
        private string _selectedEventActionPreset = NoEventActionPresetOption;
        private string _eventActionPresetName = string.Empty;
        private string _eventActionPresetStatus = string.Empty;
        private Button _eventBlendShapeDropdownButton;
        private Coroutine _eventBlendShapeDropdownRefreshCoroutine;

        [UIComponent("event-action-preset-dropdown")]
        public DropDownListSetting eventActionPresetDropdown;

        [UIComponent("event-action-preset-name-setting")]
        public StringSetting eventActionPresetNameSetting;

        [UIComponent("event-target-dropdown")]
        public DropDownListSetting eventTargetDropdown;

        [UIComponent("event-blendshape-dropdown")]
        public DropDownListSetting eventBlendShapeDropdown;

        [UIComponent("event-blendshape-manual-setting")]
        public StringSetting eventBlendShapeManualSetting;

        [UIComponent("event-action-value-setting")]
        public IncrementSetting eventActionValueSetting;

        [UIComponent("event-action-duration-setting")]
        public IncrementSetting eventActionDurationSetting;

        [UIComponent("event-action-transition-setting")]
        public IncrementSetting eventActionTransitionSetting;

        public VmcBlendShapeSettingsListViewController()
        {
            _eventTargetOptions = BuildEventTargetOptions();
        }

        [Inject]
        public void Construct(VmcBlendShapeCatalog catalog, VmcOscReceiver oscReceiver)
        {
            _catalog = catalog;
            _oscReceiver = oscReceiver;
        }

        [UIAction("#post-parse")]
        public void PostParse()
        {
            RefreshBlendShapeDropdownOptions();
            RefreshDropdown(eventTargetDropdown, _eventTargetOptions);
            RefreshEventActionPresetOptions();
            RefreshDropdown(eventActionPresetDropdown, _eventActionPresetOptions);
            EnsureEventBlendShapeDropdownButtonHandler();
            NotifySelectedEventActionProperties();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            DetachEventBlendShapeDropdownButtonHandler();
            CancelPendingEventBlendShapeDropdownRefresh();
        }

        [UIValue("enableTimeBasedExpression")]
        public bool enableTimeBasedExpression
        {
            get => PluginConfig.Instance.enableTimeBasedExpression;
            set => PluginConfig.Instance.enableTimeBasedExpression = value;
        }

        [UIValue("enableEventBasedExpression")]
        public bool enableEventBasedExpression
        {
            get => PluginConfig.Instance.enableEventBasedExpression;
            set => PluginConfig.Instance.enableEventBasedExpression = value;
        }

        [UIValue("songSpecificScript")]
        public bool songSpecificScript
        {
            get => PluginConfig.Instance.songSpecificScript;
            set => PluginConfig.Instance.songSpecificScript = value;
        }

        [UIValue("vmcHost")]
        public string vmcHost
        {
            get => PluginConfig.Instance.vmcHost;
            set => PluginConfig.Instance.vmcHost = value;
        }

        [UIValue("vmcSendPort")]
        public int vmcSendPort
        {
            get => PluginConfig.Instance.vmcSendPort;
            set => PluginConfig.Instance.vmcSendPort = value;
        }

        [UIValue("vmcListenPort")]
        public int vmcListenPort
        {
            get => PluginConfig.Instance.vmcListenPort;
            set
            {
                var normalized = Math.Max(1, Math.Min(65535, value));
                if (PluginConfig.Instance.vmcListenPort == normalized)
                {
                    return;
                }

                PluginConfig.Instance.vmcListenPort = normalized;
                RestartOscReceiverIfEnabled();
            }
        }

        [UIValue("enableOscReceiver")]
        public bool enableOscReceiver
        {
            get => PluginConfig.Instance.enableOscReceiver;
            set
            {
                PluginConfig.Instance.enableOscReceiver = value;
                _oscReceiver?.SetEnabled(value);
                NotifyPropertyChanged(nameof(discoveredBlendShapeCount));
                NotifyPropertyChanged(nameof(discoveredBlendShapesPreview));
            }
        }

        [UIValue("defaultTransition")]
        public float defaultTransition
        {
            get => PluginConfig.Instance.defaultTransition;
            set => PluginConfig.Instance.defaultTransition = Math.Max(0f, value);
        }

        [UIValue("enableGameStartEvent")]
        public bool enableGameStartEvent
        {
            get => PluginConfig.Instance.enableGameStartEvent;
            set => PluginConfig.Instance.enableGameStartEvent = value;
        }

        [UIValue("enableGameEndEvent")]
        public bool enableGameEndEvent
        {
            get => PluginConfig.Instance.enableGameEndEvent;
            set => PluginConfig.Instance.enableGameEndEvent = value;
        }

        [UIValue("enablePauseEvent")]
        public bool enablePauseEvent
        {
            get => PluginConfig.Instance.enablePauseEvent;
            set => PluginConfig.Instance.enablePauseEvent = value;
        }

        [UIValue("enableResumeEvent")]
        public bool enableResumeEvent
        {
            get => PluginConfig.Instance.enableResumeEvent;
            set => PluginConfig.Instance.enableResumeEvent = value;
        }

        [UIValue("enableComboEvent")]
        public bool enableComboEvent
        {
            get => PluginConfig.Instance.enableComboEvent;
            set => PluginConfig.Instance.enableComboEvent = value;
        }

        [UIValue("comboTriggerCount")]
        public int comboTriggerCount
        {
            get => PluginConfig.Instance.comboTriggerCount;
            set => PluginConfig.Instance.comboTriggerCount = Math.Max(1, Math.Min(9999, value));
        }

        [UIValue("enableComboDropEvent")]
        public bool enableComboDropEvent
        {
            get => PluginConfig.Instance.enableComboDropEvent;
            set => PluginConfig.Instance.enableComboDropEvent = value;
        }

        [UIValue("enableMissEvent")]
        public bool enableMissEvent
        {
            get => PluginConfig.Instance.enableMissEvent;
            set => PluginConfig.Instance.enableMissEvent = value;
        }

        [UIValue("enableBombEvent")]
        public bool enableBombEvent
        {
            get => PluginConfig.Instance.enableBombEvent;
            set => PluginConfig.Instance.enableBombEvent = value;
        }

        [UIValue("enableClearEvent")]
        public bool enableClearEvent
        {
            get => PluginConfig.Instance.enableClearEvent;
            set => PluginConfig.Instance.enableClearEvent = value;
        }

        [UIValue("enableFailEvent")]
        public bool enableFailEvent
        {
            get => PluginConfig.Instance.enableFailEvent;
            set => PluginConfig.Instance.enableFailEvent = value;
        }

        [UIValue("enableFullComboEvent")]
        public bool enableFullComboEvent
        {
            get => PluginConfig.Instance.enableFullComboEvent;
            set => PluginConfig.Instance.enableFullComboEvent = value;
        }

        [UIValue("discoveredBlendShapeCount")]
        public string discoveredBlendShapeCount
        {
            get
            {
                var baseText = $"Detected from VMC: {_catalog?.Count ?? 0}";
                return PluginConfig.Instance.enableOscReceiver
                    ? baseText
                    : baseText + " (OSC Receiver OFF)";
            }
        }

        [UIValue("discoveredBlendShapesPreview")]
        public string discoveredBlendShapesPreview
        {
            get
            {
                if (_catalog == null)
                {
                    return "(catalog unavailable)";
                }

                var names = _catalog.GetAll();
                if (names.Count == 0)
                {
                    return PluginConfig.Instance.enableOscReceiver
                        ? "No BlendShape detected yet. Ensure VMC paid version sends /VMC/Ext/Blend/Val."
                        : "OSC Receiver is OFF. Turn ON if you want to auto-detect received BlendShapes.";
                }

                var preview = names.Take(20).ToArray();
                var suffix = names.Count > 20 ? " ..." : string.Empty;
                return string.Join(", ", preview) + suffix;
            }
        }

        [UIValue("eventActionPresetOptions")]
        public List<object> eventActionPresetOptions
        {
            get
            {
                EnsureEventActionPresetOptionsInitialized();
                return _eventActionPresetOptions;
            }
        }

        [UIValue("selectedEventActionPreset")]
        public object selectedEventActionPreset
        {
            get => _selectedEventActionPreset;
            set
            {
                var presetName = NormalizePresetSelectionValue(value);
                if (string.IsNullOrWhiteSpace(presetName)
                    || string.Equals(_selectedEventActionPreset, presetName, StringComparison.Ordinal))
                {
                    return;
                }

                _selectedEventActionPreset = presetName;
                if (string.Equals(_selectedEventActionPreset, NoEventActionPresetOption, StringComparison.Ordinal))
                {
                    _eventActionPresetStatus = "Preset selection cleared.";
                    NotifyPropertyChanged(nameof(eventActionPresetStatus));
                    return;
                }

                if (TryLoadEventActionPreset(_selectedEventActionPreset, out var statusMessage))
                {
                    _eventActionPresetName = _selectedEventActionPreset;
                    RefreshBlendShapeDropdownOptions();
                    NotifySelectedEventActionProperties();
                }
                else
                {
                    _selectedEventActionPreset = NoEventActionPresetOption;
                    RefreshDropdown(eventActionPresetDropdown, _eventActionPresetOptions);
                }

                _eventActionPresetStatus = statusMessage;
                NotifyPropertyChanged(nameof(selectedEventActionPreset));
                NotifyPropertyChanged(nameof(eventActionPresetName));
                NotifyPropertyChanged(nameof(eventActionPresetStatus));
            }
        }

        [UIValue("eventActionPresetName")]
        public string eventActionPresetName
        {
            get => _eventActionPresetName;
            set => _eventActionPresetName = (value ?? string.Empty).Trim();
        }

        [UIValue("eventActionPresetStatus")]
        public string eventActionPresetStatus => _eventActionPresetStatus;

        [UIValue("eventTargetOptions")]
        public List<object> eventTargetOptions => _eventTargetOptions;

        [UIValue("selectedEventTarget")]
        public object selectedEventTarget
        {
            get => FindEventTargetOption(_selectedEventTargetKey);
            set
            {
                var resolvedKey = ResolveEventTargetKey(value);
                if (string.IsNullOrWhiteSpace(resolvedKey) || string.Equals(_selectedEventTargetKey, resolvedKey, StringComparison.Ordinal))
                {
                    return;
                }

                _selectedEventTargetKey = resolvedKey;
                NotifySelectedEventActionProperties();
            }
        }

        [UIValue("eventBlendShapeOptions")]
        public List<object> eventBlendShapeOptions
        {
            get
            {
                EnsureBlendShapeOptionsInitialized();
                return _eventBlendShapeOptions;
            }
        }

        [UIValue("eventActionBlendShape")]
        public object eventActionBlendShape
        {
            get
            {
                EnsureBlendShapeOptionsInitialized();
                var blendShapeName = GetSelectedAction().BlendShape;
                return FindBlendShapeOption(blendShapeName) ?? CreateBlendShapeOption(blendShapeName);
            }
            set
            {
                var selected = NormalizeBlendShapeName(value);
                var action = GetSelectedAction();
                action.BlendShape = selected;
                EnsureBlendShapeOptionExists(selected);
                NotifyPropertyChanged(nameof(eventActionBlendShapeManual));
                NotifyPropertyChanged(nameof(selectedEventActionSummary));
            }
        }

        [UIValue("eventActionBlendShapeManual")]
        public string eventActionBlendShapeManual
        {
            get => GetSelectedAction().BlendShape;
            set
            {
                var normalized = (value ?? string.Empty).Trim();
                var action = GetSelectedAction();
                action.BlendShape = normalized;
                EnsureBlendShapeOptionExists(normalized);
                NotifyPropertyChanged(nameof(eventActionBlendShape));
                NotifyPropertyChanged(nameof(selectedEventActionSummary));
            }
        }

        [UIValue("eventActionValue")]
        public float eventActionValue
        {
            get => GetSelectedAction().Value;
            set
            {
                GetSelectedAction().Value = Clamp01(value);
                NotifyPropertyChanged(nameof(selectedEventActionSummary));
            }
        }

        [UIValue("eventActionDuration")]
        public float eventActionDuration
        {
            get => GetSelectedAction().Duration;
            set
            {
                GetSelectedAction().Duration = Math.Max(0f, value);
                NotifyPropertyChanged(nameof(selectedEventActionSummary));
            }
        }

        [UIValue("eventActionTransition")]
        public float eventActionTransition
        {
            get => GetSelectedAction().Transition;
            set
            {
                GetSelectedAction().Transition = Math.Max(0f, value);
                NotifyPropertyChanged(nameof(selectedEventActionSummary));
            }
        }

        [UIValue("selectedEventActionSummary")]
        public string selectedEventActionSummary
        {
            get
            {
                var targetEvent = FindEventTargetOption(_selectedEventTargetKey)?.DisplayName ?? _selectedEventTargetKey;
                var action = GetSelectedAction();
                var blendShape = string.IsNullOrWhiteSpace(action.BlendShape) ? "-" : action.BlendShape;
                var transition = action.Transition > 0f ? action.Transition : PluginConfig.Instance.defaultTransition;
                return $"Target={targetEvent}, BlendShape={blendShape}, Value={action.Value:0.00}, Duration={action.Duration:0.00}s, Transition={Math.Max(0f, transition):0.00}s";
            }
        }

        [UIAction("refresh-discovered")]
        public void RefreshDiscovered()
        {
            RestartOscReceiverIfEnabled();
            RefreshBlendShapeDropdownOptions();
            NotifyPropertyChanged(nameof(discoveredBlendShapeCount));
            NotifyPropertyChanged(nameof(discoveredBlendShapesPreview));
        }

        [UIAction("reload-event-action-presets")]
        public void ReloadEventActionPresets()
        {
            RefreshEventActionPresetOptions();
            RefreshDropdown(eventActionPresetDropdown, _eventActionPresetOptions);
            _eventActionPresetStatus = "Preset list reloaded.";
            NotifyPropertyChanged(nameof(eventActionPresetStatus));
        }

        [UIAction("save-event-action-preset")]
        public void SaveEventActionPreset()
        {
            var presetName = NormalizePresetFileName(_eventActionPresetName);
            if (string.IsNullOrWhiteSpace(presetName))
            {
                _eventActionPresetStatus = "Preset Name を入力してください。";
                NotifyPropertyChanged(nameof(eventActionPresetStatus));
                return;
            }

            if (string.Equals(presetName, NoEventActionPresetOption, StringComparison.OrdinalIgnoreCase))
            {
                _eventActionPresetStatus = "その名前は使用できません。";
                NotifyPropertyChanged(nameof(eventActionPresetStatus));
                return;
            }

            try
            {
                Directory.CreateDirectory(EventActionSettingsDirectory);
                var payload = CaptureEventActionPreset();
                var path = BuildEventActionPresetPath(presetName);
                var json = JsonConvert.SerializeObject(payload, Formatting.Indented);
                File.WriteAllText(path, json);

                _selectedEventActionPreset = presetName;
                _eventActionPresetName = presetName;
                RefreshEventActionPresetOptions();
                RefreshDropdown(eventActionPresetDropdown, _eventActionPresetOptions);

                _eventActionPresetStatus = $"Saved preset: {presetName}.JSON";
                NotifyPropertyChanged(nameof(selectedEventActionPreset));
                NotifyPropertyChanged(nameof(eventActionPresetName));
                NotifyPropertyChanged(nameof(eventActionPresetStatus));
            }
            catch (Exception ex)
            {
                _eventActionPresetStatus = $"Save failed: {ex.Message}";
                NotifyPropertyChanged(nameof(eventActionPresetStatus));
            }
        }

        private void EnsureEventActionPresetOptionsInitialized()
        {
            if (_eventActionPresetOptions.Count > 0)
            {
                return;
            }

            RefreshEventActionPresetOptions();
        }

        private void RefreshEventActionPresetOptions()
        {
            var presetNames = EnumerateEventActionPresetNames();
            _eventActionPresetOptions.Clear();
            _eventActionPresetOptions.Add(NoEventActionPresetOption);

            for (var i = 0; i < presetNames.Count; i++)
            {
                _eventActionPresetOptions.Add(presetNames[i]);
            }

            if (!_eventActionPresetOptions.OfType<string>().Any(x => string.Equals(x, _selectedEventActionPreset, StringComparison.Ordinal)))
            {
                _selectedEventActionPreset = NoEventActionPresetOption;
            }

            NotifyPropertyChanged(nameof(eventActionPresetOptions));
            NotifyPropertyChanged(nameof(selectedEventActionPreset));
        }

        private List<string> EnumerateEventActionPresetNames()
        {
            try
            {
                Directory.CreateDirectory(EventActionSettingsDirectory);

                return Directory
                    .GetFiles(EventActionSettingsDirectory)
                    .Where(path => string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase))
                    .Select(path => Path.GetFileNameWithoutExtension(path))
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch (Exception ex)
            {
                _eventActionPresetStatus = $"Preset scan failed: {ex.Message}";
                NotifyPropertyChanged(nameof(eventActionPresetStatus));
                return new List<string>();
            }
        }

        private static string NormalizePresetSelectionValue(object value)
        {
            return (value?.ToString() ?? string.Empty).Trim();
        }

        private static string NormalizePresetFileName(string rawName)
        {
            var normalized = (rawName ?? string.Empty).Trim();
            if (normalized.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(0, normalized.Length - 5);
            }

            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                normalized = normalized.Replace(invalid, '_');
            }

            return normalized.Trim();
        }

        private static string BuildEventActionPresetPath(string presetName)
        {
            return Path.Combine(EventActionSettingsDirectory, $"{presetName}.JSON");
        }

        private bool TryLoadEventActionPreset(string presetName, out string statusMessage)
        {
            try
            {
                var path = BuildEventActionPresetPath(presetName);
                if (!File.Exists(path))
                {
                    statusMessage = $"Preset not found: {presetName}.JSON";
                    return false;
                }

                var json = File.ReadAllText(path);
                var payload = JsonConvert.DeserializeObject<EventActionPresetJson>(json);
                if (payload == null)
                {
                    statusMessage = $"Preset parse failed: {presetName}.JSON";
                    return false;
                }

                ApplyEventActionPreset(payload);
                statusMessage = $"Loaded preset: {presetName}.JSON";
                return true;
            }
            catch (Exception ex)
            {
                statusMessage = $"Load failed: {ex.Message}";
                return false;
            }
        }

        private static EventActionPresetJson CaptureEventActionPreset()
        {
            var config = PluginConfig.Instance;
            return new EventActionPresetJson
            {
                Version = 1,
                ComboTriggerCount = Math.Max(1, Math.Min(9999, config.comboTriggerCount)),
                GameStartEventAction = CloneAction(config.gameStartEventAction),
                GameEndEventAction = CloneAction(config.gameEndEventAction),
                PauseEventAction = CloneAction(config.pauseEventAction),
                ResumeEventAction = CloneAction(config.resumeEventAction),
                ComboEventAction = CloneAction(config.comboEventAction),
                ComboDropEventAction = CloneAction(config.comboDropEventAction),
                MissEventAction = CloneAction(config.missEventAction),
                BombEventAction = CloneAction(config.bombEventAction),
                ClearEventAction = CloneAction(config.clearEventAction),
                FailEventAction = CloneAction(config.failEventAction),
                FullComboEventAction = CloneAction(config.fullComboEventAction)
            };
        }

        private void ApplyEventActionPreset(EventActionPresetJson payload)
        {
            var config = PluginConfig.Instance;
            config.comboTriggerCount = Math.Max(1, Math.Min(9999, payload.ComboTriggerCount));
            config.gameStartEventAction = CloneAction(payload.GameStartEventAction);
            config.gameEndEventAction = CloneAction(payload.GameEndEventAction);
            config.pauseEventAction = CloneAction(payload.PauseEventAction);
            config.resumeEventAction = CloneAction(payload.ResumeEventAction);
            config.comboEventAction = CloneAction(payload.ComboEventAction);
            config.comboDropEventAction = CloneAction(payload.ComboDropEventAction);
            config.missEventAction = CloneAction(payload.MissEventAction);
            config.bombEventAction = CloneAction(payload.BombEventAction);
            config.clearEventAction = CloneAction(payload.ClearEventAction);
            config.failEventAction = CloneAction(payload.FailEventAction);
            config.fullComboEventAction = CloneAction(payload.FullComboEventAction);

            NotifyPropertyChanged(nameof(comboTriggerCount));
        }

        private static VmcBlendShapeActionConfig CloneAction(VmcBlendShapeActionConfig action)
        {
            return action?.Clone() ?? new VmcBlendShapeActionConfig();
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            if (value > 1f)
            {
                return 1f;
            }

            return value;
        }

        private void RestartOscReceiverIfEnabled()
        {
            if (!PluginConfig.Instance.enableOscReceiver)
            {
                return;
            }

            _oscReceiver?.SetEnabled(false);
            _oscReceiver?.SetEnabled(true);
        }

        private void RefreshBlendShapeDropdownOptions()
        {
            RebuildBlendShapeOptions();

            RefreshDropdown(eventBlendShapeDropdown, _eventBlendShapeOptions);
            EnsureEventBlendShapeDropdownButtonHandler();

            NotifyPropertyChanged(nameof(eventBlendShapeOptions));
            NotifyPropertyChanged(nameof(eventActionBlendShape));
            NotifyPropertyChanged(nameof(selectedEventActionSummary));
        }

        private void EnsureEventBlendShapeDropdownButtonHandler()
        {
            if (eventBlendShapeDropdown?.dropdown == null)
            {
                return;
            }

            var button = eventBlendShapeDropdown.dropdown.GetField<Button, DropdownWithTableView>("_button");
            if (button == null || ReferenceEquals(button, _eventBlendShapeDropdownButton))
            {
                return;
            }

            DetachEventBlendShapeDropdownButtonHandler();
            _eventBlendShapeDropdownButton = button;
            _eventBlendShapeDropdownButton.onClick.AddListener(HandleEventBlendShapeDropdownButtonClicked);
        }

        private void DetachEventBlendShapeDropdownButtonHandler()
        {
            if (_eventBlendShapeDropdownButton == null)
            {
                return;
            }

            _eventBlendShapeDropdownButton.onClick.RemoveListener(HandleEventBlendShapeDropdownButtonClicked);
            _eventBlendShapeDropdownButton = null;
        }

        private void HandleEventBlendShapeDropdownButtonClicked()
        {
            RefreshBlendShapeDropdownOptions();
            CancelPendingEventBlendShapeDropdownRefresh();
            _eventBlendShapeDropdownRefreshCoroutine = StartCoroutine(RefreshEventBlendShapeDropdownCoroutine());
        }

        private IEnumerator RefreshEventBlendShapeDropdownCoroutine()
        {
            yield return null;
            RefreshDropdown(eventBlendShapeDropdown, _eventBlendShapeOptions);
            _eventBlendShapeDropdownRefreshCoroutine = null;
        }

        private void CancelPendingEventBlendShapeDropdownRefresh()
        {
            if (_eventBlendShapeDropdownRefreshCoroutine == null)
            {
                return;
            }

            StopCoroutine(_eventBlendShapeDropdownRefreshCoroutine);
            _eventBlendShapeDropdownRefreshCoroutine = null;
        }

        private IEnumerable<string> EnumerateConfiguredBlendShapeNames()
        {
            var config = PluginConfig.Instance;
            yield return config.gameStartEventAction?.BlendShape;
            yield return config.gameEndEventAction?.BlendShape;
            yield return config.pauseEventAction?.BlendShape;
            yield return config.resumeEventAction?.BlendShape;
            yield return config.comboEventAction?.BlendShape;
            yield return config.comboDropEventAction?.BlendShape;
            yield return config.missEventAction?.BlendShape;
            yield return config.bombEventAction?.BlendShape;
            yield return config.clearEventAction?.BlendShape;
            yield return config.failEventAction?.BlendShape;
            yield return config.fullComboEventAction?.BlendShape;
        }

        private void EnsureBlendShapeOptionExists(string blendShape)
        {
            EnsureBlendShapeOptionsInitialized();

            if (string.IsNullOrWhiteSpace(blendShape))
            {
                return;
            }

            if (FindBlendShapeOption(blendShape) != null)
            {
                return;
            }

            _eventBlendShapeOptions.Add(CreateBlendShapeOption(blendShape));
            var ordered = _eventBlendShapeOptions
                .Cast<BlendShapeDropdownOption>()
                .OrderBy(x => x.CanonicalName, StringComparer.OrdinalIgnoreCase)
                .Cast<object>()
                .ToList();

            _eventBlendShapeOptions.Clear();
            _eventBlendShapeOptions.AddRange(ordered);
            RefreshDropdown(eventBlendShapeDropdown, _eventBlendShapeOptions);
            NotifyPropertyChanged(nameof(eventBlendShapeOptions));
            NotifyPropertyChanged(nameof(eventActionBlendShape));
        }

        private BlendShapeDropdownOption FindBlendShapeOption(string canonicalName)
        {
            EnsureBlendShapeOptionsInitialized();

            if (string.IsNullOrWhiteSpace(canonicalName))
            {
                return _eventBlendShapeOptions
                    .OfType<BlendShapeDropdownOption>()
                    .FirstOrDefault(x => string.IsNullOrWhiteSpace(x.CanonicalName));
            }

            return _eventBlendShapeOptions
                .OfType<BlendShapeDropdownOption>()
                .FirstOrDefault(x => string.Equals(x.CanonicalName, canonicalName.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private static string NormalizeBlendShapeName(object value)
        {
            if (value is BlendShapeDropdownOption option)
            {
                return option.CanonicalName;
            }

            return (value as string ?? string.Empty).Trim();
        }

        private static BlendShapeDropdownOption CreateBlendShapeOption(string canonicalName)
        {
            return new BlendShapeDropdownOption((canonicalName ?? string.Empty).Trim());
        }

        private void EnsureBlendShapeOptionsInitialized()
        {
            if (_eventBlendShapeOptions.Count > 0)
            {
                return;
            }

            RebuildBlendShapeOptions();
        }

        private void RebuildBlendShapeOptions()
        {
            var names = CollectBlendShapeNames();

            _eventBlendShapeOptions.Clear();
            _eventBlendShapeOptions.Add(CreateBlendShapeOption(string.Empty));
            for (var i = 0; i < names.Count; i++)
            {
                _eventBlendShapeOptions.Add(CreateBlendShapeOption(names[i]));
            }

            if (_eventBlendShapeOptions.Count == 1)
            {
                _eventBlendShapeOptions.Add(CreateBlendShapeOption("Neutral"));
            }
        }

        private List<string> CollectBlendShapeNames()
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < DefaultBlendShapeNames.Length; i++)
            {
                names.Add(DefaultBlendShapeNames[i]);
            }

            if (_catalog != null)
            {
                foreach (var name in _catalog.GetAll())
                {
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        names.Add(name.Trim());
                    }
                }
            }

            foreach (var configuredName in EnumerateConfiguredBlendShapeNames())
            {
                if (!string.IsNullOrWhiteSpace(configuredName))
                {
                    names.Add(configuredName.Trim());
                }
            }

            return names
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void NotifySelectedEventActionProperties()
        {
            NotifyPropertyChanged(nameof(eventActionBlendShape));
            NotifyPropertyChanged(nameof(eventActionBlendShapeManual));
            NotifyPropertyChanged(nameof(eventActionValue));
            NotifyPropertyChanged(nameof(eventActionDuration));
            NotifyPropertyChanged(nameof(eventActionTransition));
            NotifyPropertyChanged(nameof(selectedEventActionSummary));
            RefreshEventActionEditorControls();
        }

        private void RefreshEventActionEditorControls()
        {
            eventActionPresetDropdown?.ReceiveValue();
            eventActionPresetNameSetting?.ReceiveValue();
            eventTargetDropdown?.ReceiveValue();
            eventBlendShapeDropdown?.ReceiveValue();
            eventBlendShapeManualSetting?.ReceiveValue();
            eventActionValueSetting?.ReceiveValue();
            eventActionDurationSetting?.ReceiveValue();
            eventActionTransitionSetting?.ReceiveValue();
        }

        private string ResolveEventTargetKey(object value)
        {
            if (value is EventTargetOption option)
            {
                return option.Key;
            }

            var raw = value as string;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            var input = raw.Trim();
            var fromKey = _eventTargetOptions
                .OfType<EventTargetOption>()
                .FirstOrDefault(x => string.Equals(x.Key, input, StringComparison.OrdinalIgnoreCase));
            if (fromKey != null)
            {
                return fromKey.Key;
            }

            var fromDisplay = _eventTargetOptions
                .OfType<EventTargetOption>()
                .FirstOrDefault(x => string.Equals(x.DisplayName, input, StringComparison.OrdinalIgnoreCase));
            return fromDisplay?.Key;
        }

        private VmcBlendShapeActionConfig GetSelectedAction()
        {
            var config = PluginConfig.Instance;
            switch (_selectedEventTargetKey)
            {
                case EventTargetKeys.GameStart:
                    return config.gameStartEventAction;
                case EventTargetKeys.GameEnd:
                    return config.gameEndEventAction;
                case EventTargetKeys.Pause:
                    return config.pauseEventAction;
                case EventTargetKeys.Resume:
                    return config.resumeEventAction;
                case EventTargetKeys.Combo:
                    return config.comboEventAction;
                case EventTargetKeys.ComboDrop:
                    return config.comboDropEventAction;
                case EventTargetKeys.Miss:
                    return config.missEventAction;
                case EventTargetKeys.Bomb:
                    return config.bombEventAction;
                case EventTargetKeys.Clear:
                    return config.clearEventAction;
                case EventTargetKeys.Fail:
                    return config.failEventAction;
                case EventTargetKeys.FullCombo:
                    return config.fullComboEventAction;
                default:
                    return config.gameStartEventAction;
            }
        }

        private static List<object> BuildEventTargetOptions()
        {
            return new List<object>
            {
                new EventTargetOption(EventTargetKeys.GameStart, "Game Start"),
                new EventTargetOption(EventTargetKeys.GameEnd, "Game End"),
                new EventTargetOption(EventTargetKeys.Pause, "Pause"),
                new EventTargetOption(EventTargetKeys.Resume, "Resume"),
                new EventTargetOption(EventTargetKeys.Combo, "Combo"),
                new EventTargetOption(EventTargetKeys.ComboDrop, "Combo Drop"),
                new EventTargetOption(EventTargetKeys.Miss, "Miss"),
                new EventTargetOption(EventTargetKeys.Bomb, "Bomb"),
                new EventTargetOption(EventTargetKeys.Clear, "Clear"),
                new EventTargetOption(EventTargetKeys.Fail, "Fail"),
                new EventTargetOption(EventTargetKeys.FullCombo, "Full Combo")
            };
        }

        private EventTargetOption FindEventTargetOption(string key)
        {
            return _eventTargetOptions
                .OfType<EventTargetOption>()
                .FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.Ordinal))
                ?? _eventTargetOptions.OfType<EventTargetOption>().First();
        }

        private static void RefreshDropdown(DropDownListSetting dropdown, List<object> options)
        {
            if (dropdown == null)
            {
                return;
            }

            dropdown.values = options;
            dropdown.UpdateChoices();
            dropdown.ReceiveValue();
        }

        private sealed class EventActionPresetJson
        {
            [JsonProperty("version")]
            public int Version { get; set; } = 1;

            [JsonProperty("comboTriggerCount")]
            public int ComboTriggerCount { get; set; } = 50;

            [JsonProperty("gameStartEventAction")]
            public VmcBlendShapeActionConfig GameStartEventAction { get; set; } = new VmcBlendShapeActionConfig();

            [JsonProperty("gameEndEventAction")]
            public VmcBlendShapeActionConfig GameEndEventAction { get; set; } = new VmcBlendShapeActionConfig();

            [JsonProperty("pauseEventAction")]
            public VmcBlendShapeActionConfig PauseEventAction { get; set; } = new VmcBlendShapeActionConfig();

            [JsonProperty("resumeEventAction")]
            public VmcBlendShapeActionConfig ResumeEventAction { get; set; } = new VmcBlendShapeActionConfig();

            [JsonProperty("comboEventAction")]
            public VmcBlendShapeActionConfig ComboEventAction { get; set; } = new VmcBlendShapeActionConfig();

            [JsonProperty("comboDropEventAction")]
            public VmcBlendShapeActionConfig ComboDropEventAction { get; set; } = new VmcBlendShapeActionConfig();

            [JsonProperty("missEventAction")]
            public VmcBlendShapeActionConfig MissEventAction { get; set; } = new VmcBlendShapeActionConfig();

            [JsonProperty("bombEventAction")]
            public VmcBlendShapeActionConfig BombEventAction { get; set; } = new VmcBlendShapeActionConfig();

            [JsonProperty("clearEventAction")]
            public VmcBlendShapeActionConfig ClearEventAction { get; set; } = new VmcBlendShapeActionConfig();

            [JsonProperty("failEventAction")]
            public VmcBlendShapeActionConfig FailEventAction { get; set; } = new VmcBlendShapeActionConfig();

            [JsonProperty("fullComboEventAction")]
            public VmcBlendShapeActionConfig FullComboEventAction { get; set; } = new VmcBlendShapeActionConfig();
        }

        private static class EventTargetKeys
        {
            public const string GameStart = "GameStart";
            public const string GameEnd = "GameEnd";
            public const string Pause = "Pause";
            public const string Resume = "Resume";
            public const string Combo = "Combo";
            public const string ComboDrop = "ComboDrop";
            public const string Miss = "Miss";
            public const string Bomb = "Bomb";
            public const string Clear = "Clear";
            public const string Fail = "Fail";
            public const string FullCombo = "FullCombo";
        }

        private sealed class EventTargetOption
        {
            public EventTargetOption(string key, string displayName)
            {
                Key = key;
                DisplayName = displayName;
            }

            public string Key { get; }
            public string DisplayName { get; }

            public override string ToString()
            {
                return DisplayName;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(this, obj))
                {
                    return true;
                }

                if (obj is EventTargetOption option)
                {
                    return string.Equals(Key, option.Key, StringComparison.Ordinal);
                }

                if (obj is string value)
                {
                    return string.Equals(Key, value, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(DisplayName, value, StringComparison.OrdinalIgnoreCase);
                }

                return false;
            }

            public override int GetHashCode()
            {
                return StringComparer.Ordinal.GetHashCode(Key ?? string.Empty);
            }
        }

        private sealed class BlendShapeDropdownOption
        {
            public BlendShapeDropdownOption(string canonicalName)
            {
                CanonicalName = canonicalName;
            }

            public string CanonicalName { get; }

            public override string ToString()
            {
                return string.IsNullOrWhiteSpace(CanonicalName) ? "-----" : CanonicalName;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(this, obj))
                {
                    return true;
                }

                if (obj is BlendShapeDropdownOption option)
                {
                    return string.Equals(CanonicalName, option.CanonicalName, StringComparison.OrdinalIgnoreCase);
                }

                if (obj is string value)
                {
                    return string.Equals(CanonicalName, value, StringComparison.OrdinalIgnoreCase);
                }

                return false;
            }

            public override int GetHashCode()
            {
                return StringComparer.OrdinalIgnoreCase.GetHashCode(CanonicalName ?? string.Empty);
            }
        }
    }
}
