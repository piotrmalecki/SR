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
    }
}
