using VMCBlendShapeControl.Models;
using Zenject;

namespace VMCBlendShapeControl.Installers
{
    internal class VMCBlendShapeAppInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<VmcExpressionData>().AsSingle();
            Container.BindInterfacesAndSelfTo<VmcBlendShapeCatalog>().AsSingle();
            Container.BindInterfacesAndSelfTo<VmcOscSender>().AsSingle();
            Container.BindInterfacesAndSelfTo<VmcOscReceiver>().AsSingle();
            Container.BindInterfacesAndSelfTo<VmcExpressionTransitionEngine>().AsSingle();
        }
    }
}
