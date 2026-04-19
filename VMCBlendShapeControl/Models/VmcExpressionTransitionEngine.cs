using System;
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
        private readonly object _pendingActionLock = new object();
        private readonly AutoResetEvent _actionSignal = new AutoResetEvent(false);
        private readonly Dictionary<string, float> _currentValues = new Dictionary<string, float>(StringComparer.Ordinal);

        private VmcBlendShapeActionConfig _latestAction;
        private int _actionVersion;
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

            lock (_pendingActionLock)
            {
                if (_disposed)
                {
                    return;
                }

                _latestAction = action.Clone();
                Interlocked.Increment(ref _actionVersion);
            }

            _actionSignal.Set();
        }

        private void QueueWorker()
        {
            try
            {
                while (true)
                {
                    _actionSignal.WaitOne();

                    if (_disposed)
                    {
                        break;
                    }

                    while (TryDequeueLatest(out var action, out var actionVersion))
                    {
                        if (_disposed)
                        {
                            return;
                        }

                        ProcessAction(action, actionVersion);
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Transition engine stopped unexpectedly: {ex.Message}");
            }
        }

        private bool TryDequeueLatest(out VmcBlendShapeActionConfig action, out int actionVersion)
        {
            lock (_pendingActionLock)
            {
                if (_latestAction == null)
                {
                    action = null;
                    actionVersion = 0;
                    return false;
                }

                action = _latestAction;
                actionVersion = _actionVersion;
                _latestAction = null;
                return true;
            }
        }

        private void ProcessAction(VmcBlendShapeActionConfig action, int actionVersion)
        {
            if (IsCancelled(actionVersion))
            {
                return;
            }

            var blendShape = action.BlendShape.Trim();
            var targetValue = Mathf.Clamp01(action.Value);
            var transitionSec = ResolveTransitionSec(action);
            var duration = Math.Max(0f, action.Duration);

            var current = 0f;
            _currentValues.TryGetValue(blendShape, out current);

            if (duration > 0f)
            {
                var effectiveTransition = Math.Min(transitionSec, duration / 2f);
                var holdSec = Math.Max(0f, duration - (effectiveTransition * 2f));

                var forward = Transition(blendShape, current, targetValue, effectiveTransition, actionVersion);
                _currentValues[blendShape] = forward.LastValue;

                if (forward.Cancelled)
                {
                    return;
                }

                if (holdSec > 0f && SleepMs(holdSec, actionVersion))
                {
                    return;
                }

                var backToZero = Transition(blendShape, _currentValues[blendShape], 0f, effectiveTransition, actionVersion);
                _currentValues[blendShape] = backToZero.LastValue;

                if (backToZero.Cancelled)
                {
                    return;
                }

                return;
            }

            var oneWay = Transition(blendShape, current, targetValue, transitionSec, actionVersion);
            _currentValues[blendShape] = oneWay.LastValue;
        }

        private float ResolveTransitionSec(VmcBlendShapeActionConfig action)
        {
            var transitionSec = action.Transition > 0f ? action.Transition : PluginConfig.Instance.defaultTransition;
            return Math.Max(0f, transitionSec);
        }

        private TransitionResult Transition(string blendShape, float from, float to, float transitionSec, int actionVersion)
        {
            if (IsCancelled(actionVersion))
            {
                return TransitionResult.CreateCancelled(from);
            }

            if (Mathf.Abs(from - to) < 0.0001f || transitionSec <= 0f)
            {
                _sender.SendBlendValue(blendShape, to);
                return IsCancelled(actionVersion) ? TransitionResult.CreateCancelled(to) : TransitionResult.CreateCompleted(to);
            }

            var tickMs = Math.Max(4, PluginConfig.Instance.transitionTickMs);
            var totalMs = Math.Max(1, Mathf.RoundToInt(transitionSec * 1000f));
            var steps = Math.Max(1, Mathf.CeilToInt(totalMs / (float)tickMs));
            var lastValue = from;

            for (var i = 1; i <= steps; i++)
            {
                if (IsCancelled(actionVersion))
                {
                    return TransitionResult.CreateCancelled(lastValue);
                }

                var t = i / (float)steps;
                var value = Mathf.Lerp(from, to, t);
                _sender.SendBlendValue(blendShape, value);
                lastValue = value;

                if (InterruptibleSleepMs(tickMs, actionVersion))
                {
                    return TransitionResult.CreateCancelled(lastValue);
                }
            }

            return TransitionResult.CreateCompleted(lastValue);
        }

        private bool SleepMs(float sec, int actionVersion)
        {
            if (sec <= 0f)
            {
                return IsCancelled(actionVersion);
            }

            var ms = Math.Max(1, Mathf.RoundToInt(sec * 1000f));
            return InterruptibleSleepMs(ms, actionVersion);
        }

        private bool InterruptibleSleepMs(int totalMs, int actionVersion)
        {
            var remaining = totalMs;
            while (remaining > 0)
            {
                if (IsCancelled(actionVersion))
                {
                    return true;
                }

                var chunk = Math.Min(10, remaining);
                Thread.Sleep(chunk);
                remaining -= chunk;
            }

            return IsCancelled(actionVersion);
        }

        private bool IsCancelled(int actionVersion)
        {
            return _disposed || actionVersion != Volatile.Read(ref _actionVersion);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            lock (_pendingActionLock)
            {
                _latestAction = null;
                Interlocked.Increment(ref _actionVersion);
            }

            _actionSignal.Set();

            if (_worker != null && _worker.IsAlive && Thread.CurrentThread != _worker)
            {
                _worker.Join(500);
            }

            _actionSignal.Dispose();
            _worker = null;
        }

        private readonly struct TransitionResult
        {
            public readonly float LastValue;
            public readonly bool Cancelled;

            private TransitionResult(float lastValue, bool cancelled)
            {
                LastValue = lastValue;
                Cancelled = cancelled;
            }

            public static TransitionResult CreateCompleted(float value)
            {
                return new TransitionResult(value, false);
            }

            public static TransitionResult CreateCancelled(float value)
            {
                return new TransitionResult(value, true);
            }
        }
    }
}
