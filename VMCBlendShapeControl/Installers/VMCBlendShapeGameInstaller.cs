using VMCBlendShapeControl.Configuration;
using VMCBlendShapeControl.Models;
using Zenject;

namespace VMCBlendShapeControl.Installers
{
    internal class VMCBlendShapeGameInstaller : Installer
    {
        public override void InstallBindings()
        {
            if (PluginConfig.Instance.enableTimeBasedExpression)
            {
                Container.BindInterfacesAndSelfTo<VmcTimeExpressionController>().AsCached().NonLazy();
            }

            if (PluginConfig.Instance.enableEventBasedExpression)
            {
                Container.BindInterfacesAndSelfTo<VmcEventExpressionManager>().AsCached().NonLazy();
            }
        }
    }
}
