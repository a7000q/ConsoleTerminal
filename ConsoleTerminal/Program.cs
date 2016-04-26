using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace ConsoleTerminal
{
    class Program
    {
        static public Trk trk;

        static void Main(string[] args)
        {
            try
            {
                switch (Properties.Settings.Default.typeTrk)
                {
                    case "sanki":
                        trk = new serialSanki();
                        break;
                    case "topaz":
                        trk = new serialTopaz();
                        break;
                }
                Timer t = new Timer(trk.start, null, 0, 1000);

                Timer ping = new Timer(pingServer, null, 0, 10000);
            }
            catch (Exception ex)
            {
                trk.writeLogs("Ошибка в программе: " + ex.ToString());
                trk.setErrorServer("Ошибка в программе: " + ex.ToString());
            }
           
            Console.ReadKey();
        }

        private static void pingServer(object sender)
        {
            try
            {
                apiRequestServer request = new apiRequestServer();
                answerServer data = request.pingServer();
            }
            catch (Exception ex)
            {
                trk.writeLogs("Ошибка пинга на сервер: " + ex.ToString());
                trk.setErrorServer("Ошибка пинга на сервер: " + ex.ToString());
            }
        }
    }
}
