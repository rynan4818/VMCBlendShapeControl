using System;
using System.Linq;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using VMCBlendShapeControl.Configuration;
using VMCBlendShapeControl.Models;
using Zenject;

namespace VMCBlendShapeControl.UI
{
    [HotReload]
    internal class VmcBlendShapeSettingsListViewController : BSMLAutomaticViewController
    {
        private VmcBlendShapeCatalog _catalog;

        [Inject]
        public void Construct(VmcBlendShapeCatalog catalog)
        {
            _catalog = catalog;
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
            set => PluginConfig.Instance.vmcListenPort = value;
        }

        [UIValue("enableBlendShapeDiscovery")]
        public bool enableBlendShapeDiscovery
        {
            get => PluginConfig.Instance.enableBlendShapeDiscovery;
            set => PluginConfig.Instance.enableBlendShapeDiscovery = value;
        }

        [UIValue("defaultTransitionSpeed")]
        public float defaultTransitionSpeed
        {
            get => PluginConfig.Instance.defaultTransitionSpeed;
            set => PluginConfig.Instance.defaultTransitionSpeed = value;
        }

        [UIValue("defaultNeutralBlendShape")]
        public string defaultNeutralBlendShape
        {
            get => PluginConfig.Instance.defaultNeutralBlendShape;
            set => PluginConfig.Instance.defaultNeutralBlendShape = value;
        }

        [UIValue("defaultNeutralValue")]
        public float defaultNeutralValue
        {
            get => PluginConfig.Instance.defaultNeutralValue;
            set => PluginConfig.Instance.defaultNeutralValue = value;
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
            set => PluginConfig.Instance.comboTriggerCount = Math.Max(1, value);
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
        public string discoveredBlendShapeCount => $"Detected: {_catalog?.Count ?? 0}";

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
                    return "No BlendShape detected yet. Ensure VMC sends /VMC/Ext/Blend/Val.";
                }

                var preview = names.Take(20).ToArray();
                var suffix = names.Count > 20 ? " ..." : string.Empty;
                return string.Join(", ", preview) + suffix;
            }
        }

        [UIAction("refresh-discovered")]
        public void RefreshDiscovered()
        {
            NotifyPropertyChanged(nameof(discoveredBlendShapeCount));
            NotifyPropertyChanged(nameof(discoveredBlendShapesPreview));
        }
    }
}
