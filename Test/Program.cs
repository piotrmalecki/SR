using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            List<String> ipStrings = new List<string>{ "192.168.09.2",
                                                       "192.168.03.48",
                                                        "192.168.03.23"};
            var stri = ipStrings.Max(i => Convert.ToInt32(i.Split('.')[3]));
            String s = "nowy";
            Console.WriteLine(s[3]);
            Console.Read();

        }
    }
}
