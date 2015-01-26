using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WpfApplication1.Comminication
{
    public class Helpers
    {
        public static String GetTimestamp(DateTime value)
        {
            return value.ToString("yyyyMMddHHmmssffff");
        }

        public static string getMyIPAddress()
        {
            IPHostEntry host;
            string localIP = "?";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily.ToString() == "InterNetwork")
                {
                    localIP = ip.ToString();
                }
            }
            return localIP;
        }
        //jesteśmy w jednej podsieci
        public static bool isGreater(string ip1, string ip2)
        {
            return Convert.ToInt32(ip1.Split('.')[3]) > Convert.ToInt32(ip2.Split('.')[3]);
        }
        public static bool isSmaller(string ip1, string ip2)
        {
            return Convert.ToInt32(ip1.Split('.')[3]) < Convert.ToInt32(ip2.Split('.')[3]);
        }
        public static bool isEqual(string ip1, string ip2)
        {
            return Convert.ToInt32(ip1.Split('.')[3]) == Convert.ToInt32(ip2.Split('.')[3]);
        }
        public static StringBuilder typeJson(string type, string value)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;
                writer.WriteStartObject();
                writer.WritePropertyName("type");
                writer.WriteValue("get-clients");
                writer.WriteEndObject();
            }
            return sb;
        }

        public static string GetNextIPAdress(List<IPEndPoint> ipEndPointList, string startIP) 
        {
            return "";
        }
        public static IPEndPoint GetNextIPAdressIPEndPoint(List<IPEndPoint> ipEndPointList, string startIP)
        {   // jesli jest ostatni to zwroć pierwszy
            var tmp = ipEndPointList.Select(i => Convert.ToInt32(i.Address.ToString().Split('.')[3])).Max();//.Split('.')[3];
            string tmp2 = startIP.Split('.')[3];
            if (ipEndPointList.Select(i => Convert.ToInt32(i.Address.ToString().Split('.')[3])).Max() <= Convert.ToInt32(startIP.Split('.')[3]))
            {
                return ipEndPointList[0];
            }
            else
            {
                return ipEndPointList.Where(i => Convert.ToInt32(i.Address.ToString().Split('.')[3]) > Convert.ToInt32(startIP.ToString().Split('.')[3])).FirstOrDefault();
            }
            
        }
    }
}
