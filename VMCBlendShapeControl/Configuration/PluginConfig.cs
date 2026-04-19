using System.IO;
using System.Runtime.CompilerServices;
using IPA.Config.Stores;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]

namespace VMCBlendShapeControl.Configuration
{
    internal class PluginConfig
    {
        public static PluginConfig Instance { get; set; }

        private bool _enableOscReceiver = true;

        public static readonly string DefaultScriptPath = Path.Combine(IPA.Utilities.UnityGame.UserDataPath, "VMCBlendShapeControl", "DefaultVMCBlendShape.json");

        public virtual string vmcExpressionScriptPath { get; set; } = DefaultScriptPath;
        public virtual bool enableTimeBasedExpression { get; set; } = true;
        public virtual bool enableEventBasedExpression { get; set; } = true;
        public virtual bool songSpecificScript { get; set; } = true;

        public virtual string vmcHost { get; set; } = "127.0.0.1";
        public virtual int vmcSendPort { get; set; } = 39540;
        public virtual int vmcListenPort { get; set; } = 39539;
        public virtual bool enableOscReceiver
        {
            get => _enableOscReceiver;
            set => _enableOscReceiver = value;
        }

        // Backward-compatible alias for old config key.
        public virtual bool enableBlendShapeDiscovery
        {
            get => _enableOscReceiver;
            set => _enableOscReceiver = value;
        }

        public virtual float defaultTransition { get; set; } = 0.1f;
        public virtual int transitionTickMs { get; set; } = 16;

        public virtual bool enableGameStartEvent { get; set; } = false;
        public virtual VmcBlendShapeActionConfig gameStartEventAction { get; set; } = new VmcBlendShapeActionConfig();

        public virtual bool enableGameEndEvent { get; set; } = false;
        public virtual VmcBlendShapeActionConfig gameEndEventAction { get; set; } = new VmcBlendShapeActionConfig();

        public virtual bool enablePauseEvent { get; set; } = false;
        public virtual VmcBlendShapeActionConfig pauseEventAction { get; set; } = new VmcBlendShapeActionConfig();

        public virtual bool enableResumeEvent { get; set; } = false;
        public virtual VmcBlendShapeActionConfig resumeEventAction { get; set; } = new VmcBlendShapeActionConfig();

        public virtual bool enableComboEvent { get; set; } = false;
        public virtual int comboTriggerCount { get; set; } = 50;
        public virtual VmcBlendShapeActionConfig comboEventAction { get; set; } = new VmcBlendShapeActionConfig();

        public virtual bool enableComboDropEvent { get; set; } = false;
        public virtual VmcBlendShapeActionConfig comboDropEventAction { get; set; } = new VmcBlendShapeActionConfig();

        public virtual bool enableMissEvent { get; set; } = false;
        public virtual VmcBlendShapeActionConfig missEventAction { get; set; } = new VmcBlendShapeActionConfig();

        public virtual bool enableBombEvent { get; set; } = false;
        public virtual VmcBlendShapeActionConfig bombEventAction { get; set; } = new VmcBlendShapeActionConfig();

        public virtual bool enableClearEvent { get; set; } = false;
        public virtual VmcBlendShapeActionConfig clearEventAction { get; set; } = new VmcBlendShapeActionConfig();

        public virtual bool enableFailEvent { get; set; } = false;
        public virtual VmcBlendShapeActionConfig failEventAction { get; set; } = new VmcBlendShapeActionConfig();

        public virtual bool enableFullComboEvent { get; set; } = false;
        public virtual VmcBlendShapeActionConfig fullComboEventAction { get; set; } = new VmcBlendShapeActionConfig();

        public virtual void OnReload()
        {
        }

        public virtual void Changed()
        {
        }

        public virtual void CopyFrom(PluginConfig other)
        {
        }
    }
}
