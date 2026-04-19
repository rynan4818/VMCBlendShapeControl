using System.Reflection;
using HarmonyLib;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using IPALogger = IPA.Logging.Logger;
using SiraUtil.Zenject;
using VMCBlendShapeControl.Configuration;
using VMCBlendShapeControl.Installers;

namespace VMCBlendShapeControl
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal static Plugin Instance { get; private set; }
        internal static IPALogger Log { get; private set; }
        public const string HARMONY_ID = "com.github.rynan4818.VMCBlendShapeControl";

        private static Harmony _harmony;

        [Init]
        public void Init(IPALogger logger, Config conf, Zenjector zenjector)
        {
            Instance = this;
            Log = logger;
            Log.Info("VMCBlendShapeControl initialized.");

            PluginConfig.Instance = conf.Generated<PluginConfig>();

            zenjector.Install<VMCBlendShapeAppInstaller>(Location.App);
            zenjector.Install<VMCBlendShapeGameInstaller>(Location.Player);
            zenjector.Install<VMCBlendShapeMenuInstaller>(Location.Menu);
        }

        [OnStart]
        public void OnApplicationStart()
        {
            _harmony = new Harmony(HARMONY_ID);
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.Debug("OnApplicationStart");
        }

        [OnExit]
        public void OnApplicationQuit()
        {
            _harmony?.UnpatchSelf();
            Log.Debug("OnApplicationQuit");
        }
    }
}
