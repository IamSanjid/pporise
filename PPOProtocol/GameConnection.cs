using System;
using System.Threading.Tasks;
using Network;

namespace PPOProtocol
{
    public class GameConnection : Connection
    {
        private bool _useSocks;
        private int _socksVersion;
        private string _socksHost;
        private int _socksPort;
        private string _socksUser;
        private string _socksPass;
        public bool IsLoggedInToWebsite { get; private set; }
        public string Id { get; private set; }

        public event Action LoggedIn;
        public event Action<Exception> LoggingError;

        private readonly HttpConnection _httpConnection;

        public string GameVersion;
        public string KG2Value;
        public string KG1Value;
        public GameConnection(string username, int socksVersion, string socksHost, int socksPort, string socksUser, string socksPass)
        {
            _useSocks = true;
            _socksVersion = socksVersion;
            _socksHost = socksHost;
            _socksPort = socksPort;
            _socksUser = socksUser;
            _socksPass = socksPass;

            _httpConnection = new HttpConnection();
            _httpConnection.LoggedIn += HttpConnection_LoggedIn;
            _httpConnection.LoggingError += HttpConnection_LoggingError;
            Username = username;

            IsLoggedInToWebsite = false;

            Port = 9339;
            Host = "167.114.159.20";          
        }

        public GameConnection(string username)
        {
            _httpConnection = new HttpConnection();
            _httpConnection.LoggedIn += HttpConnection_LoggedIn;
            _httpConnection.LoggingError += HttpConnection_LoggingError;
            Username = username;

            IsLoggedInToWebsite = false;

            Port = 9339;
            Host = "167.114.159.20";
        }

        private void HttpConnection_LoggedIn(string arg1, string arg2, string arg3)
        {
            Id = arg1;
            Username = arg2;
            HashPassword = arg3;
            IsLoggedInToWebsite = true;
            LoggedIn?.Invoke();
        }

        private void HttpConnection_LoggingError(Exception obj)
        {
            LoggingError?.Invoke(obj);
        }
        public async Task Connect()
        {
            if (!_useSocks)
            {
                Connect(Host, Port);
            }
            else
            {
                try
                {
                    var socket = await SocksConnection.OpenConnection(_socksVersion, Host, Port, _socksHost, _socksPort, _socksUser, _socksPass);
                    Initialize(socket, _socksVersion);
                }
                catch (Exception ex)
                {
                    Close(ex);
                }
            }
        }
        public async Task PostLogin(string userName, string password)
        {
             await _httpConnection.PostLogin(userName, password);
        }
    }
}
