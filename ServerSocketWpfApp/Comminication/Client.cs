using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WpfApplication1.Comminication
{
    class ClientConnect
    {
        public String type { get; set; }

        public List<Client> clients = new List<Client>();

        public int elNo { get; set; }
        
        public ClientConnect(String _type, List<Client> _list)
        {
            type = _type;
            clients = _list;
        }
    }
    public class Client
    {
        public String id { get; set; }

        public string node { get; set; }
        public String name { get; set; }

        public Client(String _id, String _name)
        {
            id = _id;
            name = _name;
        }
    }
}
