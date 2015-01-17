using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerSocketWpfApp.Comminication
{
    public class ClientWorker
    {
        public string id { get; set; }
        public  Socket socket { get; set; }

        public ClientWorker(string _id, Socket _socket)
        {
            id = _id;
            socket = _socket;
        }
    }
}
