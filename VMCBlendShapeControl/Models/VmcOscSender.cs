using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using VMCBlendShapeControl.Configuration;
using Zenject;

namespace VMCBlendShapeControl.Models
{
    public class VmcOscSender : IInitializable, IDisposable
    {
        private readonly object _lock = new object();
        private static readonly bool IsDebugAssembly = DetermineIsDebugAssembly();

        private UdpClient _udpClient;
        private IPEndPoint _destination;
        private string _host = string.Empty;
        private int _port = -1;

        public void Initialize()
        {
            lock (_lock)
            {
                _udpClient = new UdpClient();
                UpdateDestinationIfNeeded();
            }
        }

        public void SendBlendValue(string blendShapeName, float value, bool sendApply = true)
        {
            if (string.IsNullOrWhiteSpace(blendShapeName))
            {
                return;
            }

            lock (_lock)
            {
                if (_udpClient == null)
                {
                    return;
                }

                UpdateDestinationIfNeeded();

                try
                {
                    var valPacket = VmcOscMessageUtility.BuildMessage("/VMC/Ext/Blend/Val", blendShapeName, value);
                    _udpClient.Send(valPacket, valPacket.Length, _destination);
                    LogDebugOscSend($"/VMC/Ext/Blend/Val {blendShapeName} {value:0.###}");

                    if (sendApply)
                    {
                        var applyPacket = VmcOscMessageUtility.BuildMessage("/VMC/Ext/Blend/Apply");
                        _udpClient.Send(applyPacket, applyPacket.Length, _destination);
                        LogDebugOscSend("/VMC/Ext/Blend/Apply");
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log.Warn($"VMC OSC send failed: {ex.Message}");
                }
            }
        }

        public void SendBlendValues(IReadOnlyDictionary<string, float> blendShapeValues)
        {
            if (blendShapeValues == null || blendShapeValues.Count == 0)
            {
                return;
            }

            lock (_lock)
            {
                if (_udpClient == null)
                {
                    return;
                }

                UpdateDestinationIfNeeded();

                try
                {
                    foreach (var kv in blendShapeValues)
                    {
                        if (string.IsNullOrWhiteSpace(kv.Key))
                        {
                            continue;
                        }

                        var valPacket = VmcOscMessageUtility.BuildMessage("/VMC/Ext/Blend/Val", kv.Key, kv.Value);
                        _udpClient.Send(valPacket, valPacket.Length, _destination);
                        LogDebugOscSend($"/VMC/Ext/Blend/Val {kv.Key} {kv.Value:0.###}");
                    }

                    var applyPacket = VmcOscMessageUtility.BuildMessage("/VMC/Ext/Blend/Apply");
                    _udpClient.Send(applyPacket, applyPacket.Length, _destination);
                    LogDebugOscSend("/VMC/Ext/Blend/Apply");
                }
                catch (Exception ex)
                {
                    Plugin.Log.Warn($"VMC OSC send failed: {ex.Message}");
                }
            }
        }

        private void UpdateDestinationIfNeeded()
        {
            var host = PluginConfig.Instance.vmcHost?.Trim();
            var port = PluginConfig.Instance.vmcSendPort;

            if (string.IsNullOrEmpty(host))
            {
                host = "127.0.0.1";
            }

            if (port <= 0 || port > 65535)
            {
                port = 39540;
            }

            if (host == _host && port == _port)
            {
                return;
            }

            IPAddress ipAddress = IPAddress.Loopback;
            try
            {
                var addresses = Dns.GetHostAddresses(host);
                if (addresses.Length > 0)
                {
                    ipAddress = addresses[0];
                }
            }
            catch (SocketException)
            {
                Plugin.Log.Warn($"Failed to resolve OSC host '{host}', fallback to loopback.");
            }

            _host = host;
            _port = port;
            _destination = new IPEndPoint(ipAddress, port);
        }

        private void LogDebugOscSend(string message)
        {
            if (!IsDebugAssembly)
            {
                return;
            }

            Plugin.Log.Debug($"[OSC SEND] {_host}:{_port} {message}");
        }

        private static bool DetermineIsDebugAssembly()
        {
            var attr = (DebuggableAttribute)Attribute.GetCustomAttribute(typeof(VmcOscSender).Assembly, typeof(DebuggableAttribute));
            if (attr == null)
            {
                return false;
            }

            return attr.IsJITTrackingEnabled || attr.IsJITOptimizerDisabled;
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _udpClient?.Close();
                _udpClient = null;
                _destination = null;
            }
        }
    }
}
