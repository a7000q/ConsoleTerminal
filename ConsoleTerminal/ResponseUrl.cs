using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;


namespace ConsoleTerminal
{
    class ResponseUrl
    {
        public string get(string url, Dictionary<string, string> param)
        {
            var webClient = new WebClient();
            foreach (KeyValuePair<string, string> val in param)
            {
                webClient.QueryString.Add(val.Key, val.Value);
            }

            try
            {
                var response = webClient.DownloadString(url);
                return response;
            }
            catch
            {
                Console.WriteLine("нет связи с сервером");
                return "";
            }

        }
    }
}
