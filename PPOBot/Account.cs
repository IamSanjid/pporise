using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PPOBot
{
    public class Account
    {
        public string Name { get; set; }
        public string Password { get; set; }
        public Socks Socks { get; set; }
        public HttpProxy HttpProxy { get; set; }

        [JsonIgnore]
        public string FileName { get; set; }

        public Account(string name = "")
        {
            Name = name;
            Socks = new Socks();
            HttpProxy = new HttpProxy();
        }
    }
}
