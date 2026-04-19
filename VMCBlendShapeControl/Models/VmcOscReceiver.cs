using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using VMCBlendShapeControl.Configuration;
using Zenject;

namespace VMCBlendShapeControl.Models
{
    public class VmcOscReceiver : IInitializable, IDisposable
    {
        private readonly VmcBlendShapeCatalog _catalog;
        private readonly VmcOscReceiverParser _parser = new VmcOscReceiverParser();
        private readonly object _stateLock = new object();

        private bool _disposed;
        private bool _running;
        private UdpClient _udpClient;
        private Thread _thread;

        public VmcOscReceiver(VmcBlendShapeCatalog catalog)
        {
            _catalog = catalog;
        }

        public void Initialize()
        {
            if (_disposed)
            {
                return;
            }

            SetEnabled(PluginConfig.Instance.enableOscReceiver);
        }

        public void SetEnabled(bool enabled)
        {
            if (_disposed)
            {
                return;
            }

            lock (_stateLock)
            {
                if (enabled)
                {
                    StartReceiver_NoLock();
                }
                else
                {
                    StopReceiver_NoLock();
                }
            }
        }

        private void StartReceiver_NoLock()
        {
            if (_running)
            {
                return;
            }

            try
            {
                var port = PluginConfig.Instance.vmcListenPort;
                if (port <= 0 || port > 65535)
                {
                    port = 39539;
                }

                _udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, port));
                _running = true;
                _thread = new Thread(ReceiveLoop)
                {
                    IsBackground = true,
                    Name = "VMCBlendShapeControl-Receiver"
                };
                _thread.Start();
                Plugin.Log.Notice($"VMC OSC receiver started on port {port}");
            }
            catch (Exception ex)
            {
                _running = false;
                Plugin.Log.Warn($"Failed to start VMC receiver: {ex.Message}");
            }
        }

        private void StopReceiver_NoLock()
        {
            if (!_running)
            {
                return;
            }

            _running = false;
            try
            {
                _udpClient?.Close();
            }
            catch
            {
            }
            finally
            {
                _udpClient = null;
            }

            try
            {
                if (_thread != null && _thread.IsAlive)
                {
                    _thread.Join(150);
                }
            }
            catch
            {
            }
            finally
            {
                _thread = null;
            }

            Plugin.Log.Notice("VMC OSC receiver stopped");
        }

        private void ReceiveLoop()
        {
            var remote = new IPEndPoint(IPAddress.Any, 0);
            while (!_disposed)
            {
                try
                {
                    if (!_running || _udpClient == null)
                    {
                        break;
                    }

                    if (_udpClient.Available == 0)
                    {
                        Thread.Sleep(10);
                        continue;
                    }

                    var bytes = _udpClient.Receive(ref remote);
                    if (bytes == null || bytes.Length == 0)
                    {
                        continue;
                    }

                    _parser.ParsePacket(bytes, bytes.Length, HandleMessage);
                }
                catch (SocketException)
                {
                    if (_disposed || !_running)
                    {
                        break;
                    }
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Plugin.Log.Debug($"VMC receiver parse warning: {ex.Message}");
                }
            }
        }

        private void HandleMessage(string address, System.Collections.Generic.List<object> args)
        {
            if (address == "/VMC/Ext/Blend/Val" && args != null && args.Count > 0)
            {
                if (args[0] is string blendShapeName)
                {
                    _catalog.Add(blendShapeName);
                }
            }
            else if (address == "/VMC/Ext/VRM")
            {
                _catalog.Clear();
            }
        }

        public void Dispose()
        {
            _disposed = true;
            lock (_stateLock)
            {
                StopReceiver_NoLock();
            }
        }
    }
}
