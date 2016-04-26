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
    class serialSanki:Trk
    {
        private Boolean litrSumOk = false;

        private List<byte> buffer = new List<byte>();

        private Queue<byte[]> writeCommands = new Queue<byte[]>();

        public serialSanki()
        {
            reader = new SerialPort(Properties.Settings.Default.portTrk);
            reader.BaudRate = 9600;
            reader.Parity = Parity.None;
            reader.StopBits = StopBits.Two;
            reader.DataBits = 8;
            reader.Handshake = Handshake.None;
            reader.RtsEnable = true;
            reader.ReadTimeout = 300;
            reader.WriteTimeout = 300;
            reader.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

            Thread addCommandProccessWorkThread = new Thread(new ThreadStart(addCommandProccessWork));
            addCommandProccessWorkThread.Start();

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
                Console.WriteLine("Sanki Port NOT Open" + ex.ToString());
            }
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
                        writeLogs("Ошибка записи в serialPort: " + ex.ToString());
                        setErrorServer("Ошибка записи в serialPort: " + ex.ToString());
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
                        byte[] commandT = commands.Dequeue();
                        Crc16 summ = new Crc16();

                        byte[] endAnswer = { 0x10, 0x03 };
                        byte[] startAnswer = { 0x10, 0x02 };

                        int start = PatternAt(commandT, startAnswer);
                        int end = PatternAt(commandT, endAnswer);

                        byte[] command = new byte[end - start - 5];
                        Array.Copy(commandT, start + 3, command, 0, end - start - 5);



                        if (start != -1 && end != -1)
                        {
                            writeConsoleStatus(command);
                            byte commandNumber = getCommand(summ, command, commandT, end);
                            //Console.WriteLine(BitConverter.ToString(command));
                            //Console.WriteLine(status);

                            switch (commandNumber)
                            {
                                case 0x41:
                                    fuelProccess(command);
                                    break;
                                case 0x54:
                                    endFuel(command);
                                    break;
                                case 0x53:
                                    nozleStatus(command);
                                    break;
                                case 0x43:
                                    setCountLitr(command);
                                    break;
                                default:
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
                    int s_index;
                    //Thread.Sleep(5000);
                    s_index = buffer.IndexOf(0x10);

                    //Console.WriteLine(s_index);

                    if (s_index >= 0 && buffer[s_index + 1] == 0x02 && buffer.Count > 30)
                    {
                        for (int j = 0; j <= s_index; s_index--)
                            buffer.RemoveAt(j);

                        List<byte> result = new List<byte>();
                        result.Add(0x10);
                        result.Add(0x02);

                        int i = 0;
                        while (buffer[i] != 0x10 && buffer[i + 1] != 0x03)
                        {
                            result.Add(buffer[i]);
                            i++;
                        }

                        result.Add(0x10);
                        result.Add(0x03);

                        i++;
                        int e_index = i;

                        for (int j = 0; j <= e_index; e_index--)
                            buffer.RemoveAt(j);

                        commands.Enqueue(result.ToArray());

                        //Console.WriteLine(BitConverter.ToString(result.ToArray()));
                    }
                    else if (buffer.Count > 30)
                    {
                        for (int j = 0; j <= s_index; s_index--)
                            buffer.RemoveAt(j);
                    }
                }
                catch (Exception ex)
                {
                    writeLogs("Ошибка формирования command" + ex.ToString());
                    setErrorServer("Ошибка формирования command" + ex.ToString());
                }
            }
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

            //Console.WriteLine(BitConverter.ToString(buffer.ToArray()));

           
        }

        private void setCountLitr(byte[] command)
        {
            byte[] sum_t = new byte[9];

            string an = "";
            for (int i = 0; i <= 8; i++)
                an += (char) command[i + 3];

            //string an = Encoding.ASCII.GetString(sum_t);
            //an = an.Insert(7, ",");
            sumLitr = an;

            litrSumOk = true;

            //writeLogs(an);
        }

        private void endFuel(byte[] command)
        {

            string an = Encoding.ASCII.GetString(command);

            byte[] tranz = new byte[2];

            Array.Copy(command, 2, tranz, 0, 2);

            string total_transaction_volume = an.Substring(5, 6);
            litr = total_transaction_volume.Substring(0, 4) + "," + total_transaction_volume.Substring(4, 2);

            byte command_request = 0x43;

            writeCommands.Enqueue(request(command_request, tranz));

            if (Convert.ToSingle(litr) == 0)
                nullLitr = true;

            status = "endTranzaction";
        }

        private void fuelProccess(byte[] command)
        {
            string an = Encoding.ASCII.GetString(command);
            litr = an.Substring(5, 4) + "," + an.Substring(9, 2);
        }

        private void nozleStatus(byte [] command)
        {
            byte commandNumber = getCommand(command);
            //Console.WriteLine("nozle status " + BitConverter.ToString(command));
            switch (commandNumber)
            {
                case 0x33:
                    nozle = "up";
                    litrSumOk = false;
                    break;
                case 0x31:
                    nozle = "down";
                    //Console.Clear();
                    //Console.WriteLine(BitConverter.ToString(command));
                    break;
                default:
                    break;
            }
        }

        private byte getCommand(byte[] command)
        {
            if (command[2] == Properties.Settings.Default.nozleTrk)
                return command[3];
            else
                return 0x00;
        }

        public byte getCommand(Crc16 summ, byte[] command, byte[] buffer, int end)
        {
            if (Equals(summ.ComputeChecksum(command), BitConverter.ToUInt16(buffer, end - 2)) && command[0] == Properties.Settings.Default.addressTrk)
                return command[1];
            else
                return 0x00;
        }

        public static int PatternAt(byte[] source, byte[] pattern)
        {
            for (int i = 0; i < source.Length; i++)
            {
                if (source.Skip(i).Take(pattern.Length).SequenceEqual(pattern))
                {
                    return i;
                }
            }
            return -1;
        }

        public override void start(object sender)
        {
            writeStatus();
        }

        public override void fillMaxDoza()
        {
            status = "loadDoza";

            byte command = 0x41;
            byte[] data = Encoding.ASCII.GetBytes("1P" + Properties.Settings.Default.maxDoza + "000100");

            writeCommands.Enqueue(request(command, data));
            status = "fuel";

        }

        public void writeStatus()
        {
            byte command = 0x53;
            byte[] data = { };
            writeCommands.Enqueue(request(command, data));
            GC.Collect();

            if (status == "load" && !litrSumOk)
            {
                //Thread.Sleep(200);
                command = 0x54;
                byte[] dt = { 0x31 };
                writeCommands.Enqueue(request(command, dt));
                //writeLogs(BitConverter.ToString(request(command, data)));
            }


        }

        public static byte[] request(byte command, byte[] data)
        {
            Crc16 summ = new Crc16();
            byte[] req = new byte[data.Length + 8];
            byte[] body = new byte[data.Length + 2];
            body[0] = Properties.Settings.Default.addressTrk;
            body[1] = command;
            // Console.WriteLine(BitConverter.ToString(body));
            if (data.Length != 0)
            {
                Array.Copy(data, 0, body, 2, data.Length);
            }

            req[0] = 0x10;
            req[1] = 0x02;

            Array.Copy(body, 0, req, 2, body.Length);
            //   Console.WriteLine(BitConverter.ToString(req));
            req[req.Length - 4] = summ.ComputeChecksumBytes(body)[0];
            req[req.Length - 3] = summ.ComputeChecksumBytes(body)[1];
            req[req.Length - 2] = 0x10;
            req[req.Length - 1] = 0x03;

            return req;
        }
    }
}
