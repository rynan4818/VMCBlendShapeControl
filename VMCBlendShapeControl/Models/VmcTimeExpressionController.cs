using System;
using System.Threading;
using VMCBlendShapeControl.Configuration;
using VMCBlendShapeControl.HarmonyPatches;
using Zenject;

namespace VMCBlendShapeControl.Models
{
    public class VmcTimeExpressionController : IInitializable, IDisposable
    {
        private readonly IAudioTimeSource _audioTimeSource;
        private readonly VmcExpressionData _data;
        private readonly VmcExpressionTransitionEngine _engine;

        private bool _disposed;
        private Thread _thread;

        public VmcTimeExpressionController(IAudioTimeSource audioTimeSource, VmcExpressionData data, VmcExpressionTransitionEngine engine)
        {
            _audioTimeSource = audioTimeSource;
            _data = data;
            _engine = engine;
        }

        public void Initialize()
        {
            bool loaded;
            if (PluginConfig.Instance.songSpecificScript && SongTimeEventScriptBeatmapPatch.CustomLevelScriptPath != string.Empty)
            {
                loaded = _data.LoadVmcExpressionData(SongTimeEventScriptBeatmapPatch.CustomLevelScriptPath);
            }
            else
            {
                loaded = _data.LoadVmcExpressionData();
            }

            if (!loaded)
            {
                return;
            }

            if (_data.ScriptSettings != null && _data.ScriptSettings.defaultFacialExpressionTransition > 0f)
            {
                PluginConfig.Instance.defaultTransition = _data.ScriptSettings.defaultFacialExpressionTransition;
            }

            _data.ResetEventID();

            _thread = new Thread(() =>
            {
                while (!_disposed)
                {
                    try
                    {
                        UpdateCurrentSongTime();
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log.Error(ex);
                    }
                    finally
                    {
                        Thread.Sleep(16);
                    }
                }
            })
            {
                IsBackground = true,
                Name = "VMCBlendShapeControl-Time"
            };
            _thread.Start();
        }

        private void UpdateCurrentSongTime()
        {
            var action = _data.UpdateEvent(_audioTimeSource.songTime);
            if (action != null)
            {
                _engine.EnqueueAction(action);
            }
        }

        public void Dispose()
        {
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
