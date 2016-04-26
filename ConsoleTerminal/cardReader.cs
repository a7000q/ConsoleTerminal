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

    class cardReader
    {
        public SerialPort reader;
        public string number;
        public apiRequestServer request;
        public Trk trk;

        public FileInfo cardFile = new FileInfo("cardNumber.txt");

        static SoundPlayer simpleSound = new SoundPlayer();

        public cardReader(Trk trk)
        {
            this.trk = trk;

            reader = new SerialPort(Properties.Settings.Default.portCard);
            reader.BaudRate = 9600;
            reader.Parity = Parity.None;
            reader.StopBits = StopBits.One;
            reader.DataBits = 8;
            reader.Handshake = Handshake.None;
            reader.RtsEnable = true;
            reader.ReadTimeout = 300;
            reader.WriteTimeout = 300;

            request = new apiRequestServer();

            reader.DataReceived += new SerialDataReceivedEventHandler(CardReceivedHandler);

            readCardFile();
        }

        private void readCardFile()
        {
            if (cardFile.Exists)
            {
                StreamReader txt = new StreamReader(cardFile.FullName);
                number = txt.ReadToEnd();
            }
        }

        private void writeCardFile(string number)
        { 
            StreamWriter txt = cardFile.AppendText();
            txt.Write(number);
            txt.Close();
        }

        public void CardReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            Thread.Sleep(100);
            try
            {
                try
                {
                    int bytes = reader.BytesToRead;
                    byte[] buffer = new byte[bytes];
                    reader.Read(buffer, 0, bytes);
                    number = getNumber(buffer, Properties.Settings.Default.typeCard);
                }
                catch (Exception ex)
                {
                    trk.writeLogs("Ошибка чтения из считывателя: " + ex.ToString());
                    trk.setErrorServer("Ошибка чтения из считывателя: " + ex.ToString());
                }


                if (number != null && number != "")
                {
                    reader.Close();
                    trk.status = "cardReadOk";
                    writeCardFile(number);
                    //Console.WriteLine(number);
                    //sanki.writeLogs("Карта считана");

                    try
                    {
                        answerServer answer1 = request.terminalFuelStep1(number);
                        trk.writeLogs("Запрос terminalFuelStep1  осуществлен");

                        answerServer counterTrk = request.setCounterLitr(answer1.tranzaction, trk.sumLitr);
                        trk.writeLogs("Запрос setCounterLitr  осуществлен");

                        if (answer1.status == "ok")
                        {
                            //Console.WriteLine(answer1.sections);
                            if (answer1.sections != null)
                            {
                                trk.writeLogs("Ответ terminalFuelStep1  status=ok");
                                int id_section = answer1.sections[0].id_section;
                                int tranzaction = answer1.tranzaction;
                                answerServer answer2 = request.terminalFuelStep2(tranzaction, id_section);
                                trk.writeLogs("Запрос terminalFuelStep2  осуществлен");
                                //sanki.errorCon = false;

                                if (answer2.status == "ok")
                                {
                                    trk.fillMaxDoza();
                                    trk.writeLogs("Ответ terminalFuelStep2  status=ok");
                                }
                                else
                                    noMoney();
                            }
                            else
                            {
                                Console.WriteLine("Нет доступных секций для выбора");
                                trk.writeLogs("Нет доступных секций для выбора");
                                trk.setErrorServer("Нет доступных секций для выбора");
                            }
                        }
                        else
                        {
                            noCard();
                        }
                    }
                    catch (Exception ex)
                    {
                        trk.errorConnect();
                        Console.WriteLine(ex.ToString());
                        trk.writeLogs(ex.ToString());
                        trk.setErrorServer(ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                trk.writeLogs("Ошибка чтения с карты: " + ex.ToString());
                trk.setErrorServer("Ошибка чтения с карты: " + ex.ToString());
            }
        }

        private void noMoney()
        {
            simpleSound.SoundLocation = @"Media\limit.wav";
            simpleSound.Load();
            simpleSound.Play();

            trk.status = "cardRead";

            readerOpen();
        }

        private void noCard()
        {
            simpleSound.SoundLocation = @"Media\cardEr.wav";
            simpleSound.Load();
            simpleSound.Play();

            trk.status = "cardRead";

            readerOpen();
        }

        public void readerOpen()
        {
            if (!reader.IsOpen)
            {
                try
                {
                    reader.Open();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("CardReader Port NOT Open" + ex.ToString());
                    trk.writeLogs("CardReader Port NOT Open" + ex.ToString());
                    trk.setErrorServer("CardReader Port NOT Open" + ex.ToString());
                }
            }
        }

        public static string getNumber(byte[] buffer, string type)
        {
            string result;

            switch (type)
            {
                case "hz-1050":
                    result = BitConverter.ToString(buffer);
                    break;
                case "ПС-01":
                    result = getNumberPC01(buffer);
                    break;
                default:
                    result = BitConverter.ToString(buffer);
                    break;
            }

            return result;
        }

        public static string getNumberPC01(byte[] buffer)
        {
            byte[] resultByte = new byte[4];
            string result = "";

            if (buffer[0] == 0x23 && buffer[1] == 0x43 && buffer[2] == 0x44)
            {
                int j = 0;
                for (int i = 4; i <= 7; i++)
                {
                    resultByte[j] = buffer[i];
                    j++;
                }

                result = BitConverter.ToString(resultByte);
            }
            else
                result = "";

            return result;
        }

        public void resetCardNumber()
        {
            this.number = "";
            cardFile.Delete();
        }

        ~cardReader()
        {
            reader.Close();
        }

    }
}

