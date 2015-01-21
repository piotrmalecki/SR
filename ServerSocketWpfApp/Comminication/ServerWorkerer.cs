using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using WpfApplication1.Comminication;

namespace ServerSocketWpfApp.Comminication
{
    public class ServerWorkerer
    {
        public string ip { get; set; }
        public Socket socket { get; set; }

        public List<Client> listClients = new List<Client>();

        public ServerWorkerer(string _ip, Socket _socket, List<Client> _listClients)
        {
            ip = _ip;
            socket = _socket;
            listClients = _listClients;
        }
    }
}
