using VMCBlendShapeControl.UI;
using Zenject;

namespace VMCBlendShapeControl.Installers
{
    internal class VMCBlendShapeMenuInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<VmcBlendShapeSettingsListViewController>().FromNewComponentAsViewController().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<VmcBlendShapeSettingsFlowCoordinator>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<VmcBlendShapeSettingsMenuButtonController>().AsSingle().NonLazy();
        }
    }
}
