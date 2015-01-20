using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerSocketWpfApp.Comminication
{
    public class NodeWorker
    {
        public string ip { get; set; }
        public  Socket socket { get; set; }

        public NodeWorker(string _id, Socket _socket)
        {
            ip = _id;
            socket = _socket;
        }
    }
}
