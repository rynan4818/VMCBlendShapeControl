using System;
using System.Threading;
using System.Threading.Tasks;
using IPA.Utilities.Async;
using VMCBlendShapeControl.Configuration;
using Zenject;

namespace VMCBlendShapeControl.Models
{
    public class VmcEventExpressionManager : IInitializable, IDisposable
    {
        private readonly PauseController _pauseController;
        private readonly ComboController _comboController;
        private readonly BeatmapObjectManager _beatmapObjectManager;
        private readonly ILevelEndActions _levelEndActions;
        private readonly AudioTimeSyncController _audioTimeSyncController;
        private readonly GameEnergyCounter _gameEnergyCounter;
        private readonly MultiplayerLocalActivePlayerFacade _multiplayerLocalActivePlayerFacade;
        private readonly VmcExpressionTransitionEngine _engine;

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private bool _disposed;
        private bool _isFullCombo = true;
        private bool _gameStartedTriggered;
        private int _lastCombo;
        private float _lastEnergy;

        [Inject]
        public VmcEventExpressionManager(DiContainer container, VmcExpressionTransitionEngine engine)
        {
            _pauseController = container.TryResolve<PauseController>();
            _comboController = container.TryResolve<ComboController>();
            _beatmapObjectManager = container.TryResolve<BeatmapObjectManager>();
            _levelEndActions = container.TryResolve<ILevelEndActions>();
            _audioTimeSyncController = container.TryResolve<AudioTimeSyncController>();
            _gameEnergyCounter = container.TryResolve<GameEnergyCounter>();
            _multiplayerLocalActivePlayerFacade = container.TryResolve<MultiplayerLocalActivePlayerFacade>();
            _engine = engine;
        }

        public void Initialize()
        {
            if (_pauseController != null)
            {
                _pauseController.didPauseEvent += OnGamePause;
                _pauseController.didResumeEvent += OnGameResume;
            }

            if (_comboController != null)
            {
                _comboController.comboDidChangeEvent += OnComboDidChange;
            }

            if (_beatmapObjectManager != null)
            {
                _beatmapObjectManager.noteWasCutEvent += OnNoteWasCut;
                _beatmapObjectManager.noteWasMissedEvent += OnNoteWasMissed;
            }

            if (_levelEndActions != null)
            {
                _levelEndActions.levelFinishedEvent += OnLevelFinished;
                _levelEndActions.levelFailedEvent += OnLevelFailed;
            }

            if (_gameEnergyCounter != null)
            {
                _gameEnergyCounter.gameEnergyDidChangeEvent += OnEnergyDidChange;
            }

            if (_multiplayerLocalActivePlayerFacade != null)
            {
                _multiplayerLocalActivePlayerFacade.playerDidFinishEvent += OnMultiplayerLevelFinished;
            }

            _ = UnityMainThreadTaskScheduler.Factory.StartNew(SongStartWait, _cts.Token);

            _isFullCombo = true;
            _gameStartedTriggered = false;
            _lastCombo = 0;
            _lastEnergy = _gameEnergyCounter != null ? _gameEnergyCounter.energy : 0.5f;
        }

        private async Task SongStartWait()
        {
            if (_audioTimeSyncController == null)
            {
                return;
            }

            while (_audioTimeSyncController.songTime < 0f && !_cts.IsCancellationRequested)
            {
                await Task.Delay(100);
            }

            if (_cts.IsCancellationRequested || _gameStartedTriggered)
            {
                return;
            }

            _gameStartedTriggered = true;
            if (PluginConfig.Instance.enableGameStartEvent)
            {
                EnqueueConfigAction(PluginConfig.Instance.gameStartEventAction);
            }
        }

        private void EnqueueConfigAction(VmcBlendShapeActionConfig action)
        {
            if (action != null)
            {
                _engine.EnqueueAction(action);
            }
        }

        private void OnGamePause()
        {
            if (PluginConfig.Instance.enablePauseEvent)
            {
                EnqueueConfigAction(PluginConfig.Instance.pauseEventAction);
            }
        }

        private void OnGameResume()
        {
            if (PluginConfig.Instance.enableResumeEvent)
            {
                EnqueueConfigAction(PluginConfig.Instance.resumeEventAction);
            }
        }

        private void OnComboDidChange(int combo)
        {
            if (combo < _lastCombo)
            {
                _isFullCombo = false;
                if (PluginConfig.Instance.enableComboDropEvent)
                {
                    EnqueueConfigAction(PluginConfig.Instance.comboDropEventAction);
                }
            }

            _lastCombo = combo;

            if (PluginConfig.Instance.enableComboEvent && combo > 0 && PluginConfig.Instance.comboTriggerCount > 0)
            {
                if (combo % PluginConfig.Instance.comboTriggerCount == 0)
                {
                    EnqueueConfigAction(PluginConfig.Instance.comboEventAction);
                }
            }
        }

        private void OnEnergyDidChange(float energy)
        {
            if (energy < _lastEnergy && _gameStartedTriggered)
            {
                _isFullCombo = false;
            }

            _lastEnergy = energy;
        }

        private void OnNoteWasCut(NoteController noteController, in NoteCutInfo noteCutInfo)
        {
            if (noteController.noteData.gameplayType == NoteData.GameplayType.Bomb)
            {
                if (PluginConfig.Instance.enableBombEvent)
                {
                    EnqueueConfigAction(PluginConfig.Instance.bombEventAction);
                }
            }
            else if (!noteCutInfo.allIsOK)
            {
                _isFullCombo = false;
                if (PluginConfig.Instance.enableMissEvent)
                {
                    EnqueueConfigAction(PluginConfig.Instance.missEventAction);
                }
            }
        }

        private void OnNoteWasMissed(NoteController noteController)
        {
            if (noteController.noteData.gameplayType != NoteData.GameplayType.Bomb)
            {
                _isFullCombo = false;
                if (PluginConfig.Instance.enableMissEvent)
                {
                    EnqueueConfigAction(PluginConfig.Instance.missEventAction);
                }
            }
        }

        private void OnMultiplayerLevelFinished(MultiplayerLevelCompletionResults obj)
        {
            if (PluginConfig.Instance.enableGameEndEvent)
            {
                EnqueueConfigAction(PluginConfig.Instance.gameEndEventAction);
            }

            switch (obj.playerLevelEndReason)
            {
                case MultiplayerLevelCompletionResults.MultiplayerPlayerLevelEndReason.Cleared:
                    if (PluginConfig.Instance.enableClearEvent)
                    {
                        EnqueueConfigAction(PluginConfig.Instance.clearEventAction);
                    }

                    if (_isFullCombo && PluginConfig.Instance.enableFullComboEvent)
                    {
                        EnqueueConfigAction(PluginConfig.Instance.fullComboEventAction);
                    }
                    break;
                default:
                    if (PluginConfig.Instance.enableFailEvent)
                    {
                        EnqueueConfigAction(PluginConfig.Instance.failEventAction);
                    }
                    break;
            }
        }

        private void OnLevelFinished()
        {
            if (PluginConfig.Instance.enableGameEndEvent)
            {
                EnqueueConfigAction(PluginConfig.Instance.gameEndEventAction);
            }

            if (PluginConfig.Instance.enableClearEvent)
            {
                EnqueueConfigAction(PluginConfig.Instance.clearEventAction);
            }

            if (_isFullCombo && PluginConfig.Instance.enableFullComboEvent)
            {
                EnqueueConfigAction(PluginConfig.Instance.fullComboEventAction);
            }
        }

        private void OnLevelFailed()
        {
            if (PluginConfig.Instance.enableGameEndEvent)
            {
                EnqueueConfigAction(PluginConfig.Instance.gameEndEventAction);
            }

            if (PluginConfig.Instance.enableFailEvent)
            {
                EnqueueConfigAction(PluginConfig.Instance.failEventAction);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _cts.Cancel();

            if (_pauseController != null)
            {
                _pauseController.didPauseEvent -= OnGamePause;
                _pauseController.didResumeEvent -= OnGameResume;
            }

            if (_comboController != null)
            {
                _comboController.comboDidChangeEvent -= OnComboDidChange;
            }

            if (_beatmapObjectManager != null)
            {
                _beatmapObjectManager.noteWasCutEvent -= OnNoteWasCut;
                _beatmapObjectManager.noteWasMissedEvent -= OnNoteWasMissed;
            }

            if (_levelEndActions != null)
            {
                _levelEndActions.levelFinishedEvent -= OnLevelFinished;
                _levelEndActions.levelFailedEvent -= OnLevelFailed;
            }

            if (_gameEnergyCounter != null)
            {
                _gameEnergyCounter.gameEnergyDidChangeEvent -= OnEnergyDidChange;
            }

            if (_multiplayerLocalActivePlayerFacade != null)
            {
                _multiplayerLocalActivePlayerFacade.playerDidFinishEvent -= OnMultiplayerLevelFinished;
            }

            _cts.Dispose();
        }
    }
}
