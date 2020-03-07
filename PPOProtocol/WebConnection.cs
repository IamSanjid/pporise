using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace PPOProtocol
{
    public class WebConnection
    {
        public event Action<Exception> LoggingError;
        public event Action LoggedIn;

        public HttpClient Client { get; }

        public string Id { get; private set; }
        public string Username { get; private set; }
        public string HashPassword { get; private set; }

        private string LoginWebUrl = "https://pokemon-planet.com/forums/index.php?action=login2";
        private const string UserInfoUrl = "https://pokemon-planet.com/getUserInfo.php";
        private const string WebUrl = @"https://pokemon-planet.com/";

        private CookieContainer CookieContainer { get; set; }

        public WebConnection()
        {
            CookieContainer = new CookieContainer();
            Client = new HttpClient(new HttpClientHandler
            {
                CookieContainer = CookieContainer
            });
        }

        public WebConnection(string host, int port)
        {
            CookieContainer = new CookieContainer();
            Client = new HttpClient(new HttpClientHandler
            {
                UseProxy = true,
                Proxy = new WebProxy($"http://{host}:{port}", false)
                {
                    UseDefaultCredentials = true,
                },
                CookieContainer = CookieContainer
            });
        }

        public void ParseCookies(string str)
        {
            var cookies = str.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var cookie in cookies)
            {
                var name = cookie.Trim().Split('=')[0];
                var value = cookie.Trim().Split('=')[1];
                CookieContainer.Add(new Cookie(name, value, "/", ".pokemon-planet.com"));
            }
        }

        public async void PostLogin(string user, string pass)
        {
            bool _isLoggedIn = false;
            try
            {
                var html = await Client.GetStringAsync(WebUrl);

                var document = new HtmlDocument();
                document.LoadHtml(html);

                var inputs = document.DocumentNode.Descendants("input")
                        .Where(node => node.GetAttributeValue("type", "").Equals("hidden")).ToList();
                var form = document.DocumentNode.Descendants("form")
                    .Where(node => node.GetAttributeValue("id", "").Equals("loginForm")).ToList();
                form.ForEach(node => LoginWebUrl = node.GetAttributeValue("action", ""));

                var shitName = "";
                var shitValue = "";
                if (inputs != null && inputs.Count() > 0)
                {
                    foreach (var input in inputs)
                    {
                        if (input.OuterHtml.ToLowerInvariant().Contains("cookielength")) continue;
                        input.Attributes.ToList().ForEach(att =>
                        {
                            if (att.Name == "name")
                                shitName = att.Value;
                            if (att.Name == "value")
                                shitValue = att.Value;
                        });

                    }
                }

                var responseCookies = CookieContainer.GetCookies(new Uri(LoginWebUrl)).Cast<Cookie>();
#if DEBUG
                foreach (Cookie cookie in responseCookies)
                    Console.WriteLine(cookie.Name + ": " + cookie.Value);
#endif
                var values = new Dictionary<string, string>
                {
                    { "user", user },
                    { "passwrd", pass },
                    { "cookielength", "-1" },
                    { shitName, shitValue }
                };

                var content = new FormUrlEncodedContent(values);

                var result = await Client.PostAsync(LoginWebUrl, content);

                if (result != null)
                {
                    if (result.StatusCode == HttpStatusCode.Accepted || result.StatusCode == HttpStatusCode.OK)
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
                                    Id = data1[1];
                                    Username = data2[1];
                                    HashPassword = data3[1];
#if DEBUG
                                    Console.WriteLine(Id + "-" + Username + "-" + HashPassword);
#endif
                                    _isLoggedIn = true;
                                }
                            }
                        }
                    }
                    if (!_isLoggedIn)
                        LoggingError?.Invoke(null);
                    else
                        LoggedIn?.Invoke();
                }
            }
            catch (Exception ex)
            {
                LoggingError?.Invoke(ex);
            }
        }
    }
}
