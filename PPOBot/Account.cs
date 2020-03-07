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
        public string Name { get; set; } // capitalization doesn't matter....
        public string Password { get; set; }
        public Socks Socks { get; set; }
        public HttpProxy HttpProxy { get; set; }
        public string ID { get; set; }
        public string HashPassword { get; set; }
        // the actual username Pokemone Planet saved as... including all lowercase and uppercase stuff... this matters for MD5 encryption stuff
        public string Username { get; set; }

        [JsonIgnore]
        public string FileName { get; set; }

        public Account(string name = "")
        {
            Name = name;
            Socks = new Socks();
            HttpProxy = new HttpProxy();
        }

        public void SetInfo(string id, string username, string hp)
        {
            ID = id;
            Username = username;
            HashPassword = hp;
        }
    }
}
