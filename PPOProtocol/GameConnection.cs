using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using BrightNetwork;

namespace PPOProtocol
{
    public class GameConnection : SimpleTextClient
    {
        private bool _useSocks;
        private int _socksVersion;
        private string _socksHost;
        private int _socksPort;
        private string _socksUser;
        private string _socksPass;

        private readonly IPEndPoint serverHost = new IPEndPoint(IPAddress.Parse("167.114.159.20"), 9339);

        public GameConnection() : base(new BrightClient())
        {
            PacketDelimiter = "\0";
            TextEncoding = Encoding.UTF8;
        }

        public GameConnection(int socksVersion, string socksHost, int socksPort, string socksUser, string socksPass)
            : this()
        {
            _useSocks = true;
            _socksVersion = socksVersion;
            _socksHost = socksHost;
            _socksPort = socksPort;
            _socksUser = socksUser;
            _socksPass = socksPass;
        }

        public async void Connect()
        {
            if (!_useSocks)
            {
                Connect(serverHost.Address, serverHost.Port);
            }
            else
            {
                try
                {
                    Socket socket = await SocksConnection.OpenConnection(_socksVersion, serverHost.Address, serverHost.Port, _socksHost, _socksPort, _socksUser, _socksPass);
                    Initialize(socket);
                }
                catch (Exception ex)
                {
                    Close(ex);
                }
            }
        }

        protected override string ProcessDataBeforeSending(string data)
        {
            return data;
        }

        protected override string ProcessDataBeforeReceiving(string data)
        {
            return data;
        }
    }
}
