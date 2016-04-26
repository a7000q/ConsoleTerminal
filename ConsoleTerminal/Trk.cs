using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Media;


namespace ConsoleTerminal
{
    abstract class Trk
    {
        public string status = "load";
        public Boolean serverStatus = false;
        public string nozle = "down";
        public string litr;
        public string sumLitr;

        protected Queue<byte[]> commands = new Queue<byte[]>();
        private Queue<string> logs = new Queue<string>();

        public string statusLitr;

        public Boolean nullLitr = false;

        private cardReader card;
        //static FileInfo log_file = new FileInfo("logs.txt");

        protected SerialPort reader;
        public abstract void start(object sender);
        
        public abstract void fillMaxDoza();

        Dictionary<string, Boolean> statusLog = new Dictionary<string, Boolean>();

        string log_dir;

        static SoundPlayer simpleSound = new SoundPlayer();

        public Trk()
        {
            card = new cardReader(this);

            statusLog.Add("load", true);
            statusLog.Add("cardRead", true);
            statusLog.Add("cardReadOk", true);
            statusLog.Add("loadDoza", true);
            statusLog.Add("fuel", true);
            statusLog.Add("endTranzaction", true);
            statusLog.Add("setServer", true);

            Thread work = new Thread(new ThreadStart(startTrk));
            work.Start();

            Thread workLogs = new Thread(new ThreadStart(threadWriteLogs));
            workLogs.Start();
        }

        public void setErrorServer(string text)
        {
            apiRequestServer request = new apiRequestServer();
            answerServer answer = request.setErrorServer(text);

            if (answer == null)
                status = "errorConnect";
        }

        public void startTrk()
        {
            while (true)
            {
                switch (status)
                {
                    case "load":
                        statusLoad();
                        break;
                    case "cardRead":
                        statusCardRead();
                        break;
                    case "cardReadOk":
                        cardReadOk();
                        break;
                    case "loadDoza":
                        loadDoza();
                        break;
                    case "fuel":
                        fuel();
                        break;
                    case "endTranzaction":
                        break;
                    case "setServer":
                        setServer();
                        break;
                    case "errorConnect":
                        errorConnect();
                        Thread.Sleep(1000);
                        status = "cardReadError";
                        break;
                }

                if (status != "load" && nozle == "down")
                {
                    nozleDown();
                    card.reader.Close();
                    status = "load";
                }

                if ((status == "load" && Convert.ToSingle(litr) > 0) || nullLitr)
                {
                    setServer();

                    if (serverStatus)
                    {
                        nullLitr = false;
                        litr = "000,00";
                        //card.resetCardNumber();
                        serverStatus = false;

                        nozleDown();
                        card.reader.Close();
                    }
                }
            }
        }

        

        private void cardReadOk()
        {
            if (statusLog["cardReadOk"])
            {
                writeLogs("Карта считана");
                statusLog["cardReadOk"] = false;
            }
        }

        private void fuel()
        {
            if (statusLog["fuel"])
            {
                writeLogs("Заправка началась");
                statusLog["fuel"] = false;
                statusLitr = litr;
            }

            if (statusLitr != litr)
            {
                writeLogs("Заправка: " + litr + "лит.");
                statusLitr = litr;
            }

            
        }

        public void setServer()
        {
            Thread.Sleep(3000);
            if (!serverStatus)
            {

                if (statusLog["setServer"])
                {
                    writeLogs("Заправлено " + litr + " лит.");
                    statusLog["setServer"] = false;
                }
                try
                {
                    if (card.number != null && card.number != "")
                    {
                        answerServer answer1 = card.request.terminalFuelBackStep1(card.number);
                        writeLogs("Запрос fuelBackStep1  осуществлен");

                        if (answer1.status == "ok")
                        {
                            writeLogs("Ответ fuelBackStep1 status=ok");
                            int tranzaction = answer1.tranzaction;

                            answerServer answer2 = card.request.terminalFuelBackStep2(tranzaction, litr);
                            writeLogs("Запрос fuelBackStep2  осуществлен");

                            if (answer2.status == "ok")
                            {
                                writeLogs("Ответ fuelBackStep2  status=ok");
                                status = "load";
                                writeLogs("Данные отправлены и успешно обработаны");
                                card.resetCardNumber();

                                serverStatus = true;
                                resetLogStatus();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    errorConnect();
                    Console.WriteLine(ex.ToString());
                    writeLogs(ex.ToString());
                    setErrorServer(ex.ToString());
                }
            }
        }

        public void loadDoza()
        {
            card.reader.Close();
        }

        public void resetLogStatus()
        {
            statusLog["load"] = true;
            statusLog["cardRead"] = true;
            statusLog["cardReadOk"] = true;
            statusLog["loadDoza"] = true;
            statusLog["fuel"] = true;
            statusLog["endTranzaction"] = true;
            statusLog["setServer"] = true;
        }

        public void statusCardRead()
        {
            card.readerOpen();
        }

        public void errorConnect()
        {
            simpleSound.SoundLocation = @"Media\Нет связи с сервером.wav";
            simpleSound.Load();
            simpleSound.Play();
        }

        public void statusLoad()
        {
            //Thread.CurrentThread.Join();
            switch (nozle)
            {
                case "up":
                    nozleUp();
                    break;
                default:
                    break;
            }
        }

        private void nozleDown()
        {
            Thread.Sleep(2000);

            simpleSound.SoundLocation = @"Media\end.wav";
            simpleSound.Load();
            simpleSound.Play();

            writeLogs("Пистолет повешен");
        }

        private void nozleUp()
        {
            if (status == "load")
            {
                log_dir = DateTime.Now.ToString("HH-mm-ss");
                if (DateTime.Now.ToString("HH:mm:ss tt").CompareTo("11:00:00") == -1) { simpleSound.SoundLocation = @"Media\morning.wav"; }
                if (DateTime.Now.ToString("HH:mm:ss tt").CompareTo("11:00:00") == 1 && DateTime.Now.ToString("HH:mm:ss tt").CompareTo("17:00:00") == -1) { simpleSound.SoundLocation = @"Media\day.wav"; }
                if (DateTime.Now.ToString("HH:mm:ss tt").CompareTo("17:00:00") == 1) { simpleSound.SoundLocation = @"Media\evening.wav"; }

                simpleSound.Load();
                simpleSound.Play();
                Thread.Sleep(2000);
                simpleSound.SoundLocation = @"Media\reader.wav";
                simpleSound.Load();
                simpleSound.Play();
                Console.WriteLine("Nozle Up");


                status = "cardRead";
                writeLogs("Пистолет поднят");
                writeLogs("Счетчик: " + sumLitr);
            }
        }

        public void writeLogs(string msg)
        {
            try
            {
                logs.Enqueue(msg);  
            }
            catch (Exception ex)
            {
                setErrorServer("Ошибка записи в лог файл (массив): " + ex.ToString());
            }
        }

        public void writeConsoleStatus(byte [] command)
        {
            Console.Clear();
            Console.WriteLine(status + " " + nozle + " " + litr + " " + BitConverter.ToString(command));
        }

        private void threadWriteLogs()
        {
            while (true)
            {
                try
                {
                    if (logs.Count > 0)
                    {
                        string msg = logs.Dequeue();

                        string txt = "";
                        txt += DateTime.Now.ToString() + " " + card.number + " " + msg;
                        StreamWriter write_text;

                        string date = DateTime.Now.ToString("dd-MMMM-yyyy");

                        string path = Directory.GetCurrentDirectory();
                        string dir = path + "\\logs\\" + date;

                        if (!Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }

                        dir = dir + "\\" + log_dir + ".txt";
                        FileInfo ff = new FileInfo(dir);

                        using (write_text = ff.AppendText())
                        {
                            write_text.WriteLine(txt);
                            write_text.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    setErrorServer("Ошибка записи в лог файл: " + ex.ToString());
                }
            }
            
        }

        ~Trk()
        {
            reader.Close();
        }
    }
}
