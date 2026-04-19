using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using VMCBlendShapeControl.Configuration;
using Zenject;

namespace VMCBlendShapeControl.Models
{
    public class VmcExpressionTransitionEngine : IInitializable, IDisposable
    {
        private readonly VmcOscSender _sender;
        private readonly BlockingCollection<VmcBlendShapeActionConfig> _actionQueue = new BlockingCollection<VmcBlendShapeActionConfig>();
        private readonly Dictionary<string, float> _currentValues = new Dictionary<string, float>(StringComparer.Ordinal);

        private bool _disposed;
        private Thread _worker;

        public VmcExpressionTransitionEngine(VmcOscSender sender)
        {
            _sender = sender;
        }

        public void Initialize()
        {
            _worker = new Thread(QueueWorker)
            {
                IsBackground = true,
                Name = "VMCBlendShapeControl-Transition"
            };
            _worker.Start();
        }

        public void EnqueueAction(VmcBlendShapeActionConfig action)
        {
            if (_disposed || action == null || string.IsNullOrWhiteSpace(action.BlendShape))
            {
                return;
            }

            _actionQueue.Add(action.Clone());
        }

        private void QueueWorker()
        {
            try
            {
                foreach (var action in _actionQueue.GetConsumingEnumerable())
                {
                    if (_disposed)
                    {
                        break;
                    }

                    ProcessAction(action);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Transition engine stopped unexpectedly: {ex.Message}");
            }
        }

        private void ProcessAction(VmcBlendShapeActionConfig action)
        {
            var blendShape = action.BlendShape.Trim();
            var targetValue = Mathf.Clamp01(action.Value);
            var speed = action.TransitionSpeed > 0f ? action.TransitionSpeed : PluginConfig.Instance.defaultTransitionSpeed;

            var current = 0f;
            _currentValues.TryGetValue(blendShape, out current);

            Transition(blendShape, current, targetValue, speed);
            _currentValues[blendShape] = targetValue;

            if (action.DurationSec > 0f)
            {
                SleepMs(action.DurationSec);
                Transition(blendShape, _currentValues[blendShape], 0f, speed);
                _currentValues[blendShape] = 0f;
            }

            if (action.BackToNeutralDelaySec > 0f && !string.IsNullOrWhiteSpace(PluginConfig.Instance.defaultNeutralBlendShape))
            {
                SleepMs(action.BackToNeutralDelaySec);

                var neutral = PluginConfig.Instance.defaultNeutralBlendShape.Trim();
                var neutralTarget = Mathf.Clamp01(PluginConfig.Instance.defaultNeutralValue);
                var neutralCurrent = 0f;
                _currentValues.TryGetValue(neutral, out neutralCurrent);

                Transition(neutral, neutralCurrent, neutralTarget, speed);
                _currentValues[neutral] = neutralTarget;
            }
        }

        private void Transition(string blendShape, float from, float to, float speed)
        {
            if (_disposed)
            {
                return;
            }

            if (Mathf.Abs(from - to) < 0.0001f || speed <= 0f)
            {
                _sender.SendBlendValue(blendShape, to);
                return;
            }

            var tickMs = Math.Max(4, PluginConfig.Instance.transitionTickMs);
            var tickSec = tickMs / 1000f;
            var transitionSec = 1.66f / Mathf.Max(0.01f, speed);
            var steps = Math.Max(1, Mathf.CeilToInt(transitionSec / tickSec));

            for (var i = 1; i <= steps; i++)
            {
                if (_disposed)
                {
                    return;
                }

                var t = i / (float)steps;
                var value = Mathf.Lerp(from, to, t);
                _sender.SendBlendValue(blendShape, value);
                Thread.Sleep(tickMs);
            }
        }

        private static void SleepMs(float sec)
        {
            if (sec <= 0f)
            {
                return;
            }

            var ms = Math.Max(1, Mathf.RoundToInt(sec * 1000f));
            Thread.Sleep(ms);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _actionQueue.CompleteAdding();
            _actionQueue.Dispose();
        }
    }
}
