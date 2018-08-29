using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPOBot
{
    public class HttpProxy
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public HttpProxy()
        {
            Port = -1;
        }
    }
}
