using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ConsoleTerminal
{
    class apiRequestServer
    {
        public answerServer terminalFuelStep1(string card)
        {
            Dictionary<string, string> paramsUrl = new Dictionary<string, string>();
            paramsUrl.Add("r", "api/terminal-fuel-step1");
            paramsUrl.Add("id_terminal", Properties.Settings.Default.idTerminal.ToString());
            paramsUrl.Add("id_electro", card);

            ResponseUrl client = new ResponseUrl();
            answerServer data = new answerServer();

            try
            {

                while (data.status == null)
                {
                    string response = client.get(Properties.Settings.Default.urlServer, paramsUrl);
                    data = JsonConvert.DeserializeObject<answerServer>(response);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return data;
        }

        public answerServer terminalFuelStep2(int tranzaction, int id_section)
        {
            Dictionary<string, string> paramsUrl = new Dictionary<string, string>();
            paramsUrl.Add("r", "api/terminal-fuel-step2");
            paramsUrl.Add("id_section", id_section.ToString());
            paramsUrl.Add("doza", Properties.Settings.Default.maxDoza.ToString());
            paramsUrl.Add("tranzaction", tranzaction.ToString());

            ResponseUrl client = new ResponseUrl();
            answerServer data = new answerServer();

            try
            {

                while (data.status == null)
                {
                    string response = client.get(Properties.Settings.Default.urlServer, paramsUrl);
                    data = JsonConvert.DeserializeObject<answerServer>(response);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return data;
        }

        public answerServer terminalFuelBackStep1(string card)
        {
            Dictionary<string, string> paramsUrl = new Dictionary<string, string>();
            paramsUrl.Add("r", "api/terminal-fuel-back-step1");
            paramsUrl.Add("id_terminal", Properties.Settings.Default.idTerminal.ToString());
            paramsUrl.Add("id_electro", card);

            ResponseUrl client = new ResponseUrl();
            answerServer data = new answerServer();

            try
            {

                while (data.status == null)
                {
                    string response = client.get(Properties.Settings.Default.urlServer, paramsUrl);
                    data = JsonConvert.DeserializeObject<answerServer>(response);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return data;
        }

        public answerServer terminalFuelBackStep2(int tranzaction, string doza)
        {
            Dictionary<string, string> paramsUrl = new Dictionary<string, string>();
            paramsUrl.Add("r", "api/terminal-fuel-back-step2");
            paramsUrl.Add("doza", doza);
            paramsUrl.Add("tranzaction", tranzaction.ToString());

            ResponseUrl client = new ResponseUrl();
            answerServer data = new answerServer();

            try
            {

                while (data.status == null)
                {
                    string response = client.get(Properties.Settings.Default.urlServer, paramsUrl);
                    data = JsonConvert.DeserializeObject<answerServer>(response);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return data;
        }

        public answerServer setErrorServer(string text)
        {
            Dictionary<string, string> paramsUrl = new Dictionary<string, string>();
            paramsUrl.Add("r", "api/terminal-error");
            paramsUrl.Add("id_terminal", Properties.Settings.Default.idTerminal.ToString());
            paramsUrl.Add("text", text);

            ResponseUrl client = new ResponseUrl();
            answerServer data = new answerServer();

            try
            {

                while (data.status == null)
                {
                    string response = client.get(Properties.Settings.Default.urlServer, paramsUrl);
                    data = JsonConvert.DeserializeObject<answerServer>(response);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return data;
        }

        public answerServer setCounterLitr(int id_tranzaction, string litr)
        {
            Dictionary<string, string> paramsUrl = new Dictionary<string, string>();
            paramsUrl.Add("r", "api/terminal-counter");
            paramsUrl.Add("id_terminal", Properties.Settings.Default.idTerminal.ToString());
            paramsUrl.Add("id_tranzaction", id_tranzaction.ToString());
            paramsUrl.Add("litr", litr);

            ResponseUrl client = new ResponseUrl();
            answerServer data = new answerServer();

            try
            {

                while (data.status == null)
                {
                    string response = client.get(Properties.Settings.Default.urlServer, paramsUrl);
                    data = JsonConvert.DeserializeObject<answerServer>(response);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return data;
        }

        public answerServer pingServer()
        {
            Dictionary<string, string> paramsUrl = new Dictionary<string, string>();
            paramsUrl.Add("r", "api/ping-terminal");
            paramsUrl.Add("id", Properties.Settings.Default.idTerminal.ToString());
           

            ResponseUrl client = new ResponseUrl();
            answerServer data = new answerServer();

            try
            {

                while (data.status == null)
                {
                    string response = client.get(Properties.Settings.Default.urlServer, paramsUrl);
                    data = JsonConvert.DeserializeObject<answerServer>(response);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return data;
        }

    }

    class answerServer
    {
        public string status;
        public string msg;
        public int tranzaction;

        public sectionServer[] sections;
    }

    class sectionServer
    {
        public int id_section;
        public string fuel_short;
    }
}
