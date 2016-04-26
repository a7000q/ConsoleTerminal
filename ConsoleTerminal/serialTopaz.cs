using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Ports;
using System.Threading;

namespace ConsoleTerminal
{
    class serialTopaz:Trk
    {
        private Boolean litrOk = false;
        private Boolean litrSumOk = false;
        private Boolean dolivStatus = false;
        private string dolivLitr;

        private List<byte> buffer = new List<byte>();

        private Queue<byte[]> writeCommands = new Queue<byte[]>();
        private Boolean readBuffer = false;
        
        public serialTopaz()
        {
            reader = new SerialPort(Properties.Settings.Default.portTrk);
            reader.BaudRate = 4800;
            reader.Parity = Parity.Even;
            reader.StopBits = StopBits.Two;
            reader.DataBits = 7;
            reader.Handshake = Handshake.None;
            reader.RtsEnable = true;
            reader.ReadTimeout = 300;
            reader.WriteTimeout = 300;

            Thread commandThread = new Thread(new ThreadStart(addCommandProccessWork));
            commandThread.Start();

            Thread workCommandThread = new Thread(new ThreadStart(workCommand));
            workCommandThread.Start();

            Thread workWriteCommandThread = new Thread(new ThreadStart(workWriteCommand));
            workWriteCommandThread.Start();

            try
            {
                reader.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Topaz Port NOT Open" + ex.ToString());
            }

            reader.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
        }

        public void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                int bytes = reader.BytesToRead;

                if (bytes > 0)
                {
                    byte[] bufferT = new byte[bytes];

                    reader.Read(bufferT, 0, bytes);

                    buffer.AddRange(bufferT);
                }
            }
            catch (Exception ex)
            {
                writeLogs("Ошибка чтения из SerialPort: " + ex.ToString());
                setErrorServer("Ошибка чтения из SerialPort: " + ex.ToString());
            }

            //Console.WriteLine(BitConverter.ToString(bufferT));
        }

        private void workWriteCommand()
        {
            while (true)
            {
                if (writeCommands.Count > 0)
                {
                    try
                    {
                        byte[] command = writeCommands.Dequeue();
                        reader.Write(command, 0, command.Length);
                        Thread.Sleep(300);
                        GC.Collect();
                    }
                    catch (Exception ex)
                    {
                        writeLogs("Ошибка записи в SerialPort: " + ex.ToString());
                        setErrorServer("Ошибка записи в SerialPort: " + ex.ToString());
                        
                    }
                }
            }
        }

        private void workCommand()
        {
            while (true)
            {
                try
                {
                    if (commands.Count > 0)
                    {
                        byte[] command = commands.Dequeue();

                        if (validBuffer(command))
                        {
                            writeConsoleStatus(command);
                            switch (command.Length)
                            {
                                case 7:
                                    setStatus(command);
                                    break;
                                case 9:
                                    endTranzaction(command);
                                    break;
                                case 17:
                                    getLitr(command);
                                    break;
                                case 37:
                                    getCountLitr(command);
                                    break;
                            }


                        }
                    }
                }
                catch (Exception ex)
                {
                    writeLogs("Ошибка отработки command: " + ex.ToString());
                    setErrorServer("Ошибка отработки command: " + ex.ToString());
                }
            }
        }
        
        private void addCommandProccessWork()
        {
            while (true)
            {
                try
                {
                    int index_start = buffer.IndexOf(0x7F);

                    for (int i = 0; i <= index_start; index_start--)
                        buffer.RemoveAt(i);

                    int index_end = buffer.IndexOf(0x7F);

                    if (index_end > 0)
                    {
                        byte[] result = new byte[index_end + 1];

                        result[0] = 0x7F;

                        Array.Copy(buffer.ToArray(), 0, result, 1, index_end);

                        for (int i = 0; i <= index_end; index_end--)
                            buffer.RemoveAt(i);

                        commands.Enqueue(result);

                        //Console.WriteLine(BitConverter.ToString(result));
                        //Console.Clear();
                    }
                }
                catch (Exception ex)
                {
                    writeLogs("Ошибка добавления command: " + ex.ToString());
                    setErrorServer("Ошибка добавления command: " + ex.ToString());
                }
            }

        }

        private void endTranzaction(byte [] buffer)
        {
            byte command;
            byte[] data = { };

            if (buffer[2] == 0x34 && (buffer[4] == 0x30 || buffer[4] == 0x31))
            {

                if (buffer[4] == 0x31)
                {
                    Console.WriteLine("doliv");

                    dolivStatus = true;
                }
                //Thread.Sleep(300);
                command = 0x35;
                writeCommands.Enqueue(request(command, data));

                status = "endTranzaction";

                //Thread.Sleep(300);

                command = 0x38;
                writeCommands.Enqueue(request(command, data));
            }

           

        }

        protected void getCountLitr(byte [] buffer)
        {
            int length = buffer.Length;
            int i = 2;
            int j = 0;
            int size = (buffer.Length - 2 - 3) / 4;
            byte[] data = new byte[size];

            while (i <= length && buffer[i] != 0x03 && j < size)
            {
                data[j] = buffer[i];
                i = i + 2;
                j = j + 1;
            }

            string txt = "";
            string txtLitr = BitConverter.ToString(data);
            j = 1;
            for (i = 0; i < (size - 2); i++)
            {
                txt += txtLitr[j].ToString();
                j = j + 3;
            }

            txt += ",";
            txt += txtLitr[j].ToString();
            txt += txtLitr[j + 3].ToString();

            if (status == "load")
            {
                sumLitr = txt;
                litrSumOk = true;
            }
            
        }

        private void getLitr(byte[] buffer)
        {
            if (buffer[2] == 0x30)
            {
                string an = BitConverter.ToString(buffer);

                if (dolivStatus)
                {
                    dolivLitr = an[13].ToString() + an[19].ToString() + an[25].ToString() + "," + an[31].ToString() + an[37].ToString();
                    Console.WriteLine("writeDoliv");
                }
                else
                    litr = an[13].ToString() + an[19].ToString() + an[25].ToString() + "," + an[31].ToString() + an[37].ToString();

                //Console.WriteLine(litr);
            }
        }

        private void setStatus(byte [] buffer)
        {
            byte command = buffer[2];

            switch (command)
            {
                case 0x31:
                    nozle = "up";
                    litrOk = false;
                    litrSumOk = false;
                    break;
                case 0x30:
                    nozle = "down";
                    correctLitr();
                    break;
                case 0x32:
                    confirmDoza();
                    break;
                default:
                    break;
            }
        }

        public void correctLitr()
        {
            if (status == "endTranzaction" && dolivStatus == true)
            {
                litr = (Convert.ToSingle(litr)).ToString();
               
                status = "setServer";
                //setServer();
                dolivStatus = false;
            }

            if (status == "endTranzaction")
            {

                if (Convert.ToSingle(litr) == 0)
                    nullLitr = true;
            }

            if (status == "load")
            {
                dolivLitr = "000,00";
            }
        }


        private Boolean validBuffer(byte[] buffer)
        {
            if (buffer.Length == 2)
                if (buffer[0] == 0x7F)
                {
                    if (buffer[1] != 0x02)
                        return true;
                }


            if (buffer.Length > 4)
            {
                byte sum = chekSum(buffer);

                if (sum == buffer[buffer.Length - 1] && buffer[0] == 0x7F && buffer[1] == 0x02)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }


        private void confirmDoza()
        {
            //Thread.Sleep(200);

            byte command = 0x56;
            byte[] data = { };
            writeCommands.Enqueue(request(command, data));

            status = "fuel";
            Console.WriteLine("fuel");
        }



        public override void fillMaxDoza()
        {
            status = "loadDoza";
            int doza = Properties.Settings.Default.maxDoza;

            if (doza <= 990)
            {
                byte command = 0x54;
                byte[] data = new byte[5];

                int d0 = doza / 100;
                doza = doza - d0 * 100;
                data[0] = getNumberByteTopaz(d0);

                int d1 = doza / 10;
                data[1] = getNumberByteTopaz(d1);
                doza = doza - d1 * 10;

                int d2 = doza;
                data[2] = getNumberByteTopaz(d2);

                data[3] = getNumberByteTopaz(0);
                data[4] = getNumberByteTopaz(0);

                writeCommands.Enqueue(request(command, data));
                //Thread.Sleep(100);

                data = new byte[0];
                command = 0x32;
                writeCommands.Enqueue(request(command, data));
            }
            else
            {
                Console.WriteLine("Error max doza");
            }

        }

        private byte getNumberByteTopaz(int number)
        {
            byte result = 0x30;

            switch (number)
            {
                case 0:
                    result = 0x30;
                    break;
                case 1:
                    result = 0x31;
                    break;
                case 2:
                    result = 0x32;
                    break;
                case 3:
                    result = 0x33;
                    break;
                case 4:
                    result = 0x34;
                    break;
                case 5:
                    result = 0x35;
                    break;
                case 6:
                    result = 0x36;
                    break;
                case 7:
                    result = 0x37;
                    break;
                case 8:
                    result = 0x38;
                    break;
                case 9:
                    result = 0x39;
                    break;
            }

            return result;
        }

        public void writeStatus()
        {
            byte[] data = { };
            byte command = 0x31;


            writeCommands.Enqueue(request(command, data));
            GC.Collect();
            

            if (status == "load" && !litrSumOk && !dolivStatus)
            {
                //Thread.Sleep(100);
                command = 0x36;
                writeCommands.Enqueue(request(command, data));
            }

            if (status == "fuel" || status == "endTranzaction")
            {
                //Thread.Sleep(100);
                command = 0x34                                                                                                                                                                                                      ;
                writeCommands.Enqueue(request(command, data));
            }
        }


        public static byte[] request(byte command, byte[] data)
        {
            byte[] result = new byte[9 + data.Length * 2];
            result[0] = 0x7F;
            result[1] = 0x02;

            result[2] = 0x23;
            result[3] = 0x7F - 0x23;

            byte chekSum = (byte)(0x23 ^ command);

            result[4] = command;
            result[5] = (byte)(0x7F - command);

            int j = 5;

            //Console.WriteLine(result.Length.ToString());

            if (data.Length != 0)
            {
                j = 6;
                for (int i = 0; i < data.Length; i++)
                {
                    result[j] = data[i];
                    result[j + 1] = (byte)(0x7F - data[i]);
                    j = j + 2;

                    chekSum ^= data[i];
                }

                j = j - 1;
            }


            result[j + 1] = 0x03;
            result[j + 2] = 0x03;

            chekSum ^= 0x03;
            chekSum |= 0x40;

            result[j + 3] = chekSum;

            return result;
        }

        private byte chekSum(byte[] buffer)
        {
            int lenght = buffer.Length;

            byte result = buffer[2];
            int i = 4;


            while (i < lenght && buffer[i] != 0x03)
            {
                result = (byte)(result ^ buffer[i]);
                i = i + 2;
            }


            result ^= 0x03;
            result |= 0x40;

            //Console.WriteLine(i-1);
            return result;
        }



        public override void start(object sender)
        {
            writeStatus();
            
            //Console.WriteLine(BitConverter.ToString(buffer.ToArray()));
        }
    }
}
