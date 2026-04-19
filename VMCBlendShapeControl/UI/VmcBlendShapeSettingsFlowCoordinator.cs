using BeatSaberMarkupLanguage;
using HMUI;
using Zenject;

namespace VMCBlendShapeControl.UI
{
    internal class VmcBlendShapeSettingsFlowCoordinator : FlowCoordinator
    {
        private VmcBlendShapeSettingsListViewController _listViewController;

        [Inject]
        public void Construct(VmcBlendShapeSettingsListViewController listViewController)
        {
            _listViewController = listViewController;
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            if (firstActivation)
            {
                SetTitle("VMC BlendShape Control");
                showBackButton = true;
                ProvideInitialViewControllers(_listViewController);
            }
        }

        protected override void BackButtonWasPressed(ViewController topViewController)
        {
            BeatSaberUI.MainFlowCoordinator.DismissFlowCoordinator(this);
            base.BackButtonWasPressed(topViewController);
        }
    }
}
