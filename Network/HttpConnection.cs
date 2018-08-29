using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Network
{
    public class HttpConnection
    {
        public event Action<string, string, string> LoggedIn;
        public event Action<Exception> LoggingError;
        public HttpClient Client;
        private string _username;
        private string _passwrod;
        private string _id;
        public string Id => _id;

        private const string LoginWebUrl = "http://pokemon-planet.com/forums/index.php?action=login2";
        private const string UserInfoUrl = "http://www.pokemon-planet.com/getUserInfo.php";

        public HttpConnection()
        {
            Client = new HttpClient();
        }

        public HttpConnection(string host, int port)
        {
            Client = new HttpClient
            (
                new HttpClientHandler
                {
                    UseProxy = true,
                    Proxy = new WebProxy($"{host}:{port}", false)
                }
            );
        }

        public async Task PostLogin(string user, string pass)
        {
            try
            {
                var values = new Dictionary<string, string>
                {
                    { "user", user },
                    { "passwrd", pass }
                };

                var content = new FormUrlEncodedContent(values);

                var response = await Client.PostAsync(LoginWebUrl, content);
                if (response is null is false)
                {
                    if (response != null && (response.StatusCode == HttpStatusCode.Accepted || response.StatusCode == HttpStatusCode.OK))
                    {
                        var getResponse = await Client.GetStringAsync(UserInfoUrl);
                        if (!string.IsNullOrEmpty(getResponse))
                        {
                            var responsePars = getResponse
                                .Split('&');
                            if (responsePars.Length > 2)
                            {
                                var data1 = responsePars[0].Split('=');
                                var data2 = responsePars[1].Split('=');
                                var data3 = responsePars[2].Split('=');
                                if (data1.Length > 0 && data2.Length > 0 && data3.Length > 0)
                                {
                                    _id = data1[1];
                                    _username = data2[1];
                                    _passwrod = data3[1];
                                    Console.WriteLine(_id + "-" + _username + "-" + _passwrod);
                                    LoggedIn?.Invoke(_id, _username, _passwrod);
                                    return;
                                }
                            }
                        }
                    }
                    LoggingError?.Invoke(null);
                }
            }
            catch (Exception ex)
            {
                LoggingError?.Invoke(ex);
            }
        }
    }
}
