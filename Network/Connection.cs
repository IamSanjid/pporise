using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace Network
{
    public class Connection
    {
        private Exception _closingException;
        private int _countForServerDisconnections; //Wondering why we needed? It's just an easy simple beginner way to solve the disconnection and reconnection thing....
        private IPEndPoint _endPoint;
        private bool _isClosed;
        private bool _isConnected;
        private ExecutionPlan _waitForReconnect;
        public int Port { get; set; }
        public string Host { get; set; }

        private readonly Queue<string> _pendingPackets = new Queue<string>();
        private readonly byte[] _receiveBuffer;

        private string _receiveBufferString = string.Empty;

        private Socket _socket;
        private Socket _tempSocket;

        public string Username { get; set; }
        public string HashPassword { get; set; }

        private bool _wasDisconnected;

        public int SocketVersion = 4;

        public int ActiveRoomId = 1;
        private readonly string _initialHashTest;

        public Connection()
        {
            _receiveBuffer = new byte[4096];
            WasConnected = false;
            DataReceived += Client_DataReceived;
            _initialHashTest = CalcMd5("whycantwebefriends");
            _tempSocket = null;
        }

        public bool IsConnected => WasConnected;
        public bool IsLoggedIn { get; private set; }

        private bool WasConnected { get; set; }
        public event Action Connected;
        public event Action<Exception> Disconnected;
        public event Action<byte[]> DataReceived;
        public event Action<string> PacketReceived;
        public event Action<string> LogMessage;
        public event Action JoinedRoom;
        public event Action SuccessfullyAuthenticated;

        public void Login(string zone, string name, string pass)
        {
            var loc3 = "tsys";
            var loc2 = $"<login z=\'{zone}\'><nick><![CDATA[{name}]]></nick><pword><![CDATA[{pass}]]></pword></login>";
            Send(loc3, "login", 0, loc2);
        }

        public static string GenerateRandomString(int newLength)
        {
            const string loc5 = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var loc2 = loc5.ToCharArray();
            var loc3 = "";
            var random = new Random();
            for (var i = 0; i < newLength; ++i)
            {
                loc3 += loc2[(int)Math.Floor(random.NextDouble() * loc2.Length)];
            }
            return loc3;
        }

        public void Update()
        {
            ReceivePendingPackets();
            if (_wasDisconnected)
            {
                _wasDisconnected = false;
                Disconnected?.Invoke(_closingException);
            }
        }

        public static string CalcMd5(string str)
        {
            var encodedPassword = new UTF8Encoding().GetBytes(str);

            // need MD5 to calculate the hash
            var hash = ((HashAlgorithm) CryptoConfig.CreateFromName("MD5")).ComputeHash(encodedPassword);
            // string representation (similar to UNIX format)
            var encoded = BitConverter.ToString(hash)
                // without dashes
                .Replace("-", string.Empty)
                // make lowercase
                .ToLower();

            return encoded;
        }

        public void SendXtMessage(string xtName = "", string cmdName = "", ArrayList ps = null, string type = "",
            int roomId = -1)
        {
            if (type == "")
                type = "xml";

            // ReSharper disable once RedundantCheckBeforeAssignment
            if (ActiveRoomId != roomId)
                roomId = ActiveRoomId;
            switch (type)
            {
                case "xml":
                    string loc11;
                    loc11 = "txt";
                    var list = new ArrayList();
                    list.Add($"name:{xtName}");
                    list.Add($"cmd:{cmdName}");
                    string srializedText;
                    srializedText = ps is null is false ? ObjectSerilizer.Serialize(list, ps) : ObjectSerilizer.Serialize(list);
#if DEBUG
                Console.WriteLine("xml:" + srializedText);
#endif
                    var loc12 = $"<![CDATA[{srializedText}]]>";
                    Send(loc11, "xtReq", roomId, loc12);
                    break;
                case "str":
                    var loc4 =
                        $"{rawProtocolSeparator}xt{rawProtocolSeparator}{xtName}{rawProtocolSeparator}{cmdName}{rawProtocolSeparator}{1}{rawProtocolSeparator}";
                    if (ps != null)
                        if (ps.Count > 0)
                            foreach (var p in ps)
                                loc4 = loc4 + (p + rawProtocolSeparator);
#if DEBUG
                Console.WriteLine(loc4);
#endif
                    Send(loc4);
                    break;
            }
        }

        public void Connect(string address, int port, string name = "", string hashPass = "")
        {
            IPAddress ip;
            if (!IPAddress.TryParse(address, out ip)) Close();
            if (_initialHashTest == "bf2db69d7395cf9a4d4277ba5c063629" && _initialHashTest != null)
                BeginConnect(ip, port);
            else
                LogMessage?.Invoke(
                    "Error! Something went wrong while calculating the MD5. Please contact to the developers.");
        }

        public void Send(string packet)
        {
            var data = Encoding.ASCII.GetBytes(packet + "\0");
            BeginSend(data);
        }

        public void Logout()
        {
            Close();
        }

        private void Client_DataReceived(byte[] data)
        {
            var text = Encoding.ASCII.GetString(data);
            if (IsLoggedIn is false)
            {
                HandleDataForLoggingIn(text);
            }
            else
            {
                _receiveBufferString += text;
                ExtractPackets();
            }
        }

        public void Initialize(Socket socket)
        {
            _endPoint = (IPEndPoint) socket.RemoteEndPoint;
            _tempSocket = socket;
            _socket = socket;
            BeginReceive();
            _isConnected = true;
        }

        public void BeginConnect(IPAddress address, int port)
        {
            if (!_isConnected && !_isClosed)
            {
                _isConnected = true;
                try
                {
                    _endPoint = new IPEndPoint(address, port);
                    _socket = new Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    _socket.BeginConnect(_endPoint, ConnectCallback, null);
                }
                catch (Exception ex)
                {
                    Close(ex);
                }
            }
        }

        private void ReceivePendingPackets()
        {
            bool hasReceived;
            do
            {
                string packet = null;
                lock (_pendingPackets)
                {
                    if (_pendingPackets.Count > 0) packet = _pendingPackets.Dequeue();
                }

                hasReceived = false;
                if (packet != null)
                {
                    hasReceived = true;
                    PacketReceived?.Invoke(packet);
                }
            } while (hasReceived);
        }

        public void BeginSend(byte[] data)
        {
            try
            {
                _socket.BeginSend(data, 0, data.Length, SocketFlags.None, SendCallback, data.Length);
            }
            catch (Exception ex)
            {
                Close(ex);
            }
        }

        public void Close(Exception error = null)
        {
            if (!_isClosed)
            {
                IsLoggedIn = false;
                _isClosed = true;
                try
                {
                    if (_socket != null)
                    {
                        if (_socket.Connected)
                            _socket.Shutdown(SocketShutdown.Both);
                        _socket.Close();
                        _socket.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    ex = new AggregateException(error, ex);
                    _closingException = ex;
                }

                _wasDisconnected = true;
            }
        }

        private void BeginReceive()
        {
            try
            {
                if (_socket != null)
                {
                    _socket.BeginReceive(_receiveBuffer, 0, _receiveBuffer.Length, SocketFlags.None, ReceiveCallback,
                        null);
                }
            }
            catch (Exception ex)
            {
                Close(ex);
            }
        }

        private void ConnectCallback(IAsyncResult result)
        {
            try
            {
                _socket.EndConnect(result);
                if (_countForServerDisconnections < 1)
                    Send("<policy-file-request/>");
            }
            catch (Exception ex)
            {
                Close(ex);
                return;
            }

            BeginReceive();
        }

        private void SendCallback(IAsyncResult result)
        {
            try
            {
                var bytesSent = _socket.EndSend(result);
                if (bytesSent != (int) result.AsyncState) Console.WriteLine("Closing...");
            }
            catch (Exception ex)
            {
                Close(ex);
            }
        }

        private void HandleDataForLoggingIn(string packet)
        {
            if (packet.Length > 0)
            {
#if DEBUG
                Console.WriteLine("Got Data from Server:");
                Console.WriteLine(packet);
#endif
                if (packet[0] == '<' && !IsLoggedIn)
                {
                    if (packet.ToLowerInvariant().Contains("cross-domain"))
                    {
                        _countForServerDisconnections++;
                        if (_countForServerDisconnections == 1)
                        {
                            if (_socket != null)
                            {
                                _socket.BeginDisconnect(true, DisconnectCallback, _socket);
                            }
                        }
                        else if (_countForServerDisconnections >= 2)
                        {
                            var _loc3_ = "tsys";
                            var loc2 = $"<ver v=\'{Version}\' />";
                            Send(_loc3_, "verChk", 0, loc2);
                            _waitForReconnect?.Abort();
                        }
                    }
                    else if (packet.ToLowerInvariant().Contains("apiok"))
                    {
                        WasConnected = true;
                        Connected?.Invoke();
                        LogMessage?.Invoke("Sending authentication to the server....");
                        Login(Zone, Username, HashPassword);
                    }
                    else if (packet.Contains(Username) && packet.Contains("xt")
                                                        && packet.Contains("xtRes") &&
                                                        packet.Contains("_cmd") &&
                                                        packet.Contains("name")
                                                        && packet.Contains("login"))
                    {
                        SuccessfullyAuthenticated?.Invoke();
                        GetRoomList();
                    }
                    else if (packet.Contains("rmList"))
                    {
                        AutoJoin();
                    }
                    else if (packet.Contains("joinOK"))
                    {
                        IsLoggedIn = true;
                        JoinedRoom?.Invoke();
                    }
                }
            }
        }

        private void DisconnectCallback(IAsyncResult ar)
        {
            if (ar.IsCompleted)
            {
                _waitForReconnect = ExecutionPlan.Delay(1000, () =>
                {
                    Reconnect();
                });
            }
        }

        private async void Reconnect()
        {
            if (_tempSocket != null)
            {
                if (_tempSocket.RemoteEndPoint != null)
                {
                    // ReSharper disable once PossibleNullReferenceException
                    var newTempSocket = await SocksConnection.OpenConnection(SocketVersion, Host, Port,
                        ((IPEndPoint) _tempSocket.RemoteEndPoint)?.Address.ToString(),
                        ((IPEndPoint) _tempSocket.RemoteEndPoint).Port);
#if DEBUG
                    Console.WriteLine($"Using Proxy server to connect to the game server. Address: {((IPEndPoint)_tempSocket.RemoteEndPoint).Address} Port: {((IPEndPoint)_tempSocket.RemoteEndPoint).Port}");
#endif
                    Initialize(newTempSocket);
                }
            }
            else
            {
                _socket = new Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _socket.BeginConnect(_endPoint, ConnectCallback, null);
            }
        }

        private void ExtractPackets()
        {
            bool hasExtracted;
            do
            {
                hasExtracted = ExtractPendingPacket();
            } while (hasExtracted);
        }

        private bool ExtractPendingPacket()
        {
            var pos = _receiveBufferString.IndexOf(PacketDelimiter, StringComparison.Ordinal);
            if (pos >= 0)
            {
                var packet = _receiveBufferString.Substring(0, pos);
                _receiveBufferString = _receiveBufferString.Substring(pos + PacketDelimiter.Length);
                lock (_pendingPackets)
                {
                    _pendingPackets.Enqueue(packet);
                }

                return true;
            }

            return false;
        }

        public void Send(string header, string action, object fromRoom, string message)
        {
            var loc3 = makeHeader(header);
            loc3 = $"{loc3}<body action=\'{action}\' r=\'{fromRoom}\'>{message}</body>{closeHeader()}";
            Send(loc3);
#if DEBUG
            Console.WriteLine($"Sent: {loc3}");
#endif
        }

        private string closeHeader()
        {
            return "</msg>";
        }

        private void AutoJoin()
        {
            var _loc2_ = "tsys";
            Send(_loc2_, "autoJoin", -1, "");
        }

        private void GetRoomList()
        {
            var _loc2_ = "tsys";
            Send(_loc2_, "getRmList", -1, "");
        }

        private string makeHeader(string headerObj)
        {
            dynamic loc2 = $"<msg {headerObj[0]}=\'{headerObj.Substring(1)}\'>";
            return loc2;
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            if (_socket is null) return;
            int bytesRead;
            try
            {
                if (_socket.Connected && _socket != null)
                    bytesRead = _socket.EndReceive(result);
                else
                    return;
            }
            catch (Exception ex)
            {
                Close(ex);
                return;
            }

            var data = new byte[bytesRead];
            Array.Copy(_receiveBuffer, data, bytesRead);
            DataReceived?.Invoke(data);
            if (_countForServerDisconnections > 1)
            {
                BeginReceive();
            }
        }

#region

        private readonly string PacketDelimiter = "\0";

        private readonly string rawProtocolSeparator = "`";

        private const string Zone = "PokemonPlanet";

        public int Version = 163;

#endregion
    }
}