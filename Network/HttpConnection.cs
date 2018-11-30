using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace Network
{
    public class HttpConnection
    {
        public event Action<Exception> LoggingError;

        public readonly HttpClient Client;

        public string Id { get; private set; }
        public string Username { get; private set; }
        public string HashPassword { get; private set; }

        private bool _usingProxy;

        private const string LoginWebUrl = "http://pokemon-planet.com/forums/index.php?action=login2";
        private const string UserInfoUrl = "http://pokemon-planet.com/getUserInfo.php";
        private const string WebUrl = @"http://pokemon-planet.com/";

        private CookieContainer _cookieContainer { get; set; }

        public bool IsLoggedIn { get; set; }

        public HttpConnection()
        {
            _cookieContainer = new CookieContainer();
            Client = new HttpClient(new HttpClientHandler
            {
                CookieContainer = _cookieContainer
            });
            _usingProxy = false;
        }

        public HttpConnection(string host, int port)
        {
            _cookieContainer = new CookieContainer();
            Client = new HttpClient(new HttpClientHandler
            {
                UseProxy = true,
                Proxy = new WebProxy($"{host}:{port}"),
                CookieContainer = _cookieContainer
            });
            _usingProxy = true;
        }

        public async Task PostLogin(string user, string pass)
        {
            try
            {
                //creating sessions..
                var html = await Client.GetStringAsync(WebUrl);
                var document = new HtmlDocument();
                document.LoadHtml(html);

                var inputs = document.DocumentNode.Descendants("input")
                        .Where(node => node.GetAttributeValue("type", "").Equals("hidden")).ToList();
                var shitName = "";
                var shitValue = "";
                if (inputs != null && inputs.Count() > 0)
                {
                    foreach (var input in inputs)
                    {
                        if (input.OuterHtml.ToLowerInvariant().Contains("cookielength")) continue;
                        input.Attributes.ToList().ForEach(att => {
                            if (att.Name == "name")
                                shitName = att.Value;
                            if (att.Name == "value")
                                shitValue = att.Value;
                        });
                        
                    }
                }

                IEnumerable<Cookie> responseCookies = _cookieContainer.GetCookies(new Uri(LoginWebUrl)).Cast<Cookie>();
                foreach (Cookie cookie in responseCookies)
                    Console.WriteLine(cookie.Name + ": " + cookie.Value);

                var values = new Dictionary<string, string>
                {
                    { "user", user },
                    { "passwrd", pass },
                    { "cookielength", "-1" },
                    { shitName, shitValue }
                };

                var content = new FormUrlEncodedContent(values);

                var result = await Client.PostAsync(LoginWebUrl, content);

                if (result is null is false)
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
                                    Console.WriteLine(Id + "-" + Username + "-" + HashPassword);
                                    IsLoggedIn = true;
                                    return;
                                }
                            }
                        }
                    }
                    IsLoggedIn = false;
                    LoggingError?.Invoke(null);
                }
            }
            catch (Exception ex)
            {
                IsLoggedIn = false;
                LoggingError?.Invoke(ex);
            }
        }

        private async Task<HttpResponseMessage> SendPost(FormUrlEncodedContent content, string url)
        {
            var cookieContainer = _cookieContainer;
            //HttpClientHandler handler = null;
            //if (_port != -1 && !string.IsNullOrEmpty(_host))
            //    handler = new HttpClientHandler { UseProxy = true, Proxy = new WebProxy($"{_host}:{_port}"), CookieContainer = cookieContainer };
            //else
            //    handler = new HttpClientHandler { CookieContainer = cookieContainer };

            //using (var client = new HttpClient(handler, true))
            //{

            //}
            var message = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };

            // Header stuff
            //Following like Google Chrome ;).. I suggest not to touch anything below coz all info were generated by Google chrome..
            message.Headers.Add("Host", "www.pokemon-planet.com");
            message.Headers.Add("Cache-Control", "max-age=0");
            message.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.102 Safari/537.36");
            message.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
            message.Headers.Add("Accept-Language", "en-US;q=0.5,en;q=0.3");
            //message.Headers.Add("Referer", "http://pokemon-planet.com/"); // default base url is http://pokemon-planet.com/ you can change according to your need...
            if (_usingProxy)
                message.Headers.Add("Proxy-Authorization", "Basic SEZhNjQ2NjQyZTJlMDZjYjM0NWZkYzAyOGU3YTM0YjYwMS5oNzgzb2hhdzA5amRmMDpIRmE2NDY2NDJlMmUwNmNiMzQ1ZmRjMDI4ZTdhMzRiNjAxLmg3ODIzOWhk");
            message.Headers.Add("Connection", "keep-alive");
            //message.Headers.Add("Cookie", "__cfduid=ddd337116e0d3d4b2a99eaf16a1a79bcf1543416280; cf_clearance=848dcd25e7e484dd9bb7fe5425e1bb5f29264654-1543416304-604800-250; SMFCookie42=a%3A4%3A%7Bi%3A0%3Bs%3A7%3A%221377791%22%3Bi%3A1%3Bs%3A40%3A%22e3e66a226ae891a90cffc4680e346d481d903356%22%3Bi%3A2%3Bi%3A1732817396%3Bi%3A3%3Bi%3A2%3B%7D; PHPSESSID=5iojhn6qhs61irbcprh4lls2d6");
            message.Headers.Add("Upgrade-Insecure-Requests", "1");

            //Cookie stuff
            //cookieContainer.Add(new Cookie("PHPSESSID", "tt80377f00g6u2pk2d6je1rq30", "/", ".pokemon-planet.com"));
            //cookieContainer.Add(new Cookie("SMFCookie42", "a%3A4%3A%7Bi%3A0%3Bs%3A7%3A%221329075%22%3Bi%3A1%3Bs%3A40%3A%22dee7011dd34e212132e92577ca762b2112137807%22%3Bi%3A2%3Bi%3A1728804395%3Bi%3A3%3Bi%3A2%3B%7D", "/", ".pokemon-planet.com"));
            //cookieContainer.Add(new Cookie("__cfduid", "ddd337116e0d3d4b2a99eaf16a1a79bcf1543416280", "/", ".pokemon-planet.com") { HttpOnly = true }); // we can update the expire date as we want :D
            //cookieContainer.Add(new Cookie("cf_clearance", "848dcd25e7e484dd9bb7fe5425e1bb5f29264654-1543416304-604800-250", "/", ".pokemon-planet.com") { HttpOnly = true });

            //sending those datas...
            var result = await Client.SendAsync(message);
            result.EnsureSuccessStatusCode();




            return result;
        }

        private async Task<HttpResponseMessage> SendGet(string url)
        {
            //HttpClientHandler handler = null;
            //if (_port != -1 && !string.IsNullOrEmpty(_host))
            //    handler = new HttpClientHandler { UseProxy = true, Proxy = new WebProxy($"{_host}:{_port}"), CookieContainer = cookieContainer };
            //else
            //    handler = new HttpClientHandler { CookieContainer = cookieContainer };

            //using (var client = new HttpClient(handler, true))
            //{

            //}
            var message = new HttpRequestMessage(HttpMethod.Get, url);

            // Header stuff
            //Following like Google Chrome ;).. I suggest not to touch anything below coz all info were generated by Google chrome..
            message.Headers.Add("Host", "www.pokemon-planet.com");
            message.Headers.Add("Cache-Control", "max-age=0");
            message.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.102 Safari/537.36");
            message.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
            message.Headers.Add("Accept-Language", "en-US;q=0.5,en;q=0.3");
            //message.Headers.Add("Referer", "http://pokemon-planet.com/"); // default base url is http://pokemon-planet.com/ you can change according to your need...
            if (_usingProxy)
                message.Headers.Add("Proxy-Authorization", "Basic SEZhNjQ2NjQyZTJlMDZjYjM0NWZkYzAyOGU3YTM0YjYwMS5oNzgzb2hhdzA5amRmMDpIRmE2NDY2NDJlMmUwNmNiMzQ1ZmRjMDI4ZTdhMzRiNjAxLmg3ODIzOWhk");
            message.Headers.Add("Connection", "keep-alive");
            //message.Headers.Add("Cookie", "__cfduid=ddd337116e0d3d4b2a99eaf16a1a79bcf1543416280; cf_clearance=848dcd25e7e484dd9bb7fe5425e1bb5f29264654-1543416304-604800-250; SMFCookie42=a%3A4%3A%7Bi%3A0%3Bs%3A7%3A%221377791%22%3Bi%3A1%3Bs%3A40%3A%22e3e66a226ae891a90cffc4680e346d481d903356%22%3Bi%3A2%3Bi%3A1732817396%3Bi%3A3%3Bi%3A2%3B%7D; PHPSESSID=5iojhn6qhs61irbcprh4lls2d6");
            message.Headers.Add("Upgrade-Insecure-Requests", "1");

            //Cookie stuff
            //cookieContainer.Add(new Cookie("PHPSESSID", "qcbod2deocn2a2dfb619mpesm6", "/", ".pokemon-planet.com"));
            //_cookieContainer.Add(new Cookie("SMFCookie42", "a%3A4%3A%7Bi%3A0%3Bs%3A7%3A%221377791%22%3Bi%3A1%3Bs%3A40%3A%22e3e66a226ae891a90cffc4680e346d481d903356%22%3Bi%3A2%3Bi%3A1732817396%3Bi%3A3%3Bi%3A2%3B%7D", "/", ".pokemon-planet.com"));
            //cookieContainer.Add(new Cookie("__cfduid", "ddd337116e0d3d4b2a99eaf16a1a79bcf1543416280", "/", ".pokemon-planet.com") { HttpOnly = true }); // we can update the expire date as we want :D
            //cookieContainer.Add(new Cookie("cf_clearance", "848dcd25e7e484dd9bb7fe5425e1bb5f29264654-1543416304-604800-250", "/", ".pokemon-planet.com") { HttpOnly = true });
            IEnumerable<Cookie> responseCookies = _cookieContainer.GetCookies(new Uri(LoginWebUrl)).Cast<Cookie>();
            foreach (Cookie cookie in responseCookies)
                Console.WriteLine(cookie.Name + ": " + cookie.Value);
            //sending those datas...
            var result = await Client.SendAsync(message);
            result.EnsureSuccessStatusCode();

            return result;
        }
    }
}
