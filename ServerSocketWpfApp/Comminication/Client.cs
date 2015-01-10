using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApplication1.Comminication
{
    class ClientConnect
    {
        public String type { get; set; }

        public List<Client> list = new List<Client>();
        public ClientConnect(String _type, List<Client> _list)
        {
            type = _type;
            list = _list;
        }
    }
    public class Client
    {
        public String id { get; set; }

        public String name { get; set; }
        public Client(String _id, String _name)
        {
            id = _id;
            name = _name;
        }
    }
}
