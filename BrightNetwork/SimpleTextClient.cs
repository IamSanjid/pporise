using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace BrightNetwork
{
    public class SimpleTextClient
    {
        public event Action Connected;
        public event Action<Exception> Disconnected;
        public event Action<string> PacketReceived;

        protected string PacketDelimiter = "\n";
        protected Encoding TextEncoding = Encoding.UTF8;

        protected readonly BrightClient Client;

        private string _receiveBuffer = string.Empty;
        private Queue<string> _pendingPackets = new Queue<string>();

        private bool _wasConnected;
        private bool _wasDisconnected;
        private Exception _closingException;

        public bool IsConnected
        {
            get { return Client.IsConnected; }
        }

        public IPAddress RemoteIPAddress
        {
            get { return Client.RemoteIPAddress; }
        }

        public SimpleTextClient(BrightClient client)
        {
            Client = client;

            client.Connected += Client_Connected;
            client.Disconnected += Client_Disconnected;
            client.DataReceived += Client_DataReceived;
        }

        public void Connect(IPAddress address, int port)
        {
            Client.BeginConnect(address, port);
        }

        public void Initialize(Socket socket)
        {
            Client.Initialize(socket);
        }

        public void Update()
        {
            if (_wasConnected)
            {
                _wasConnected = false;
                if (ConfirmConnection())
                {
                    Connected?.Invoke();
                }
            }
            ReceivePendingPackets();
            if (_wasDisconnected)
            {
                _wasDisconnected = false;
                Disconnected?.Invoke(_closingException);
            }
        }

        public void Send(string packet)
        {
            packet = ProcessPacketBeforeSending(packet);
            byte[] data = TextEncoding.GetBytes(ProcessDataBeforeSending(packet + PacketDelimiter));
            if (data.Length > 0)
            {
                Client.BeginSend(data);
            }
        }

        public void Close(Exception error = null)
        {
            Client.Close(error);
        }

        protected virtual string ProcessDataBeforeSending(string data)
        {
            return data;
        }

        protected virtual string ProcessDataBeforeReceiving(string data)
        {
            return data;
        }

        protected virtual string ProcessPacketBeforeSending(string packet)
        {
            return packet;
        }

        protected virtual string ProcessPacketBeforeReceiving(string packet)
        {
            return packet;
        }

        protected virtual bool ConfirmConnection()
        {
            return true;
        }

        protected virtual bool ConfirmDisconnection(Exception error)
        {
            return true;
        }

        private void ReceivePendingPackets()
        {
            bool hasReceived;
            do
            {
                string packet = null;
                lock (_pendingPackets)
                {
                    if (_pendingPackets.Count > 0)
                    {
                        packet = _pendingPackets.Dequeue();
                    }
                }
                hasReceived = false;
                if (packet != null)
                {
                    hasReceived = true;
                    PacketReceived?.Invoke(packet);
                }
            }
            while (hasReceived);
        }

        private void Client_Connected()
        {
            _wasConnected = ConfirmConnection();
        }

        private void Client_Disconnected(Exception ex)
        {
            if (ConfirmDisconnection(ex))
            {
                _wasDisconnected = true;
                _closingException = ex;
            }
        }

        private void Client_DataReceived(byte[] data)
        {
            string text = ProcessDataBeforeReceiving(TextEncoding.GetString(data));
            if (text.Length > 0)
            {
                _receiveBuffer += text;
                ExtractPackets();
            }
        }

        private void ExtractPackets()
        {
            bool hasExtracted;
            do
            {
                hasExtracted = ExtractPendingPacket();
            }
            while (hasExtracted);
        }

        private bool ExtractPendingPacket()
        {
            int pos = _receiveBuffer.IndexOf(PacketDelimiter);
            if (pos >= 0)
            {
                string packet = _receiveBuffer.Substring(0, pos);
                _receiveBuffer = _receiveBuffer.Substring(pos + PacketDelimiter.Length);
                lock (_pendingPackets)
                {
                    _pendingPackets.Enqueue(ProcessPacketBeforeReceiving(packet));
                }
                return true;
            }
            return false;
        }
    }
}