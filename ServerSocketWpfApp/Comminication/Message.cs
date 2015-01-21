using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApplication1.Comminication
{
    public class Message
    {
        public string type { get; set; }
        public Client clientFrom { get; set; }
        public Client clientTo { get; set; }
        public string message { get; set; }
        public string timestamp { get; set; }

        public int elNo { get; set; }

        public Message(string _type, Client _clientFrom, Client _clientTo, string _message, string _timestamp)
        {
            this.type = _type;
            this.clientFrom = _clientFrom;
            this.clientTo = _clientTo;
            this.message = _message;
            this.timestamp = _timestamp;
        }

    }
}
