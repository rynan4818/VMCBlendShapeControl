using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using VMCBlendShapeControl.Configuration;
using Zenject;

namespace VMCBlendShapeControl.Models
{
    public class VmcOscSender : IInitializable, IDisposable
    {
        private readonly object _lock = new object();

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

                    if (sendApply)
                    {
                        var applyPacket = VmcOscMessageUtility.BuildMessage("/VMC/Ext/Blend/Apply");
                        _udpClient.Send(applyPacket, applyPacket.Length, _destination);
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
                    }

                    var applyPacket = VmcOscMessageUtility.BuildMessage("/VMC/Ext/Blend/Apply");
                    _udpClient.Send(applyPacket, applyPacket.Length, _destination);
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

            IPAddress ipAddress;
            if (!IPAddress.TryParse(host, out ipAddress))
            {
                var addresses = Dns.GetHostAddresses(host);
                ipAddress = addresses.Length > 0 ? addresses[0] : IPAddress.Loopback;
            }

            _host = host;
            _port = port;
            _destination = new IPEndPoint(ipAddress, port);
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
