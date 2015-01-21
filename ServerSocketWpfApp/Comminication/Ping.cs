using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerSocketWpfApp.Comminication
{
    public class Ping
    {
        public string type { get; set; }

        public int elNo { get; set; }

        public Ping(string _type, int _elNo)
        {
            type = _type;
            elNo = _elNo;
        }
    }
}
