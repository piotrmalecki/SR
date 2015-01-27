using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerSocketWpfApp
{
    public class ElectionBreak
    {
        public string type { get; set; }
        public int elNo { get; set; }
        public string server { get; set; }
        public ElectionBreak(string _type, int _elNo, string _ipSerwer)
        {
            type = _type;
            elNo = _elNo;
            server = _ipSerwer;
        }
    }
}
