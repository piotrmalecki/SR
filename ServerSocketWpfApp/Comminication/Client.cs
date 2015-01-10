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

    }
    public class Client
    {
        public int id { get; set; }

        public String name { get; set; }
        public Client(int _id, String _name)
        {
            id = _id;
            name = _name;
        }
    }
}
