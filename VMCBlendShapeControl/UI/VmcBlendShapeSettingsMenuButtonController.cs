using System;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.MenuButtons;
using Zenject;

namespace VMCBlendShapeControl.UI
{
    internal class VmcBlendShapeSettingsMenuButtonController : IInitializable, IDisposable
    {
        private readonly VmcBlendShapeSettingsFlowCoordinator _flowCoordinator;
        private MenuButton _menuButton;

        [Inject]
        public VmcBlendShapeSettingsMenuButtonController(VmcBlendShapeSettingsFlowCoordinator flowCoordinator)
        {
            _flowCoordinator = flowCoordinator;
        }

        public void Initialize()
        {
            _menuButton = new MenuButton("VMC BlendShape Control", "Configure VMC BlendShape Control settings.", ShowFlowCoordinator);
            MenuButtons.instance?.RegisterButton(_menuButton);
        }

        public void Dispose()
        {
            if (_menuButton != null)
            {
                MenuButtons.instance?.UnregisterButton(_menuButton);
                _menuButton = null;
            }
        }

        private void ShowFlowCoordinator()
        {
            BeatSaberUI.MainFlowCoordinator.PresentFlowCoordinator(_flowCoordinator);
        }
    }
}
