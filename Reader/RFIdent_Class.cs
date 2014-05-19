using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Collections.Concurrent;
using System.Timers;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Collections.Specialized;
using System.Threading;

namespace SensMaster
{
    /// <summary>
    /// 
    /// </summary>
    class RFIdent_Class
    {
        class Reader_Params
        {
            public byte Read_Type = 0x00; 
            public byte Current_Read_Memory_OP = 0x00;
            public OrderedDictionary Tag_EPC_Checklist =  new OrderedDictionary(10);
            public OrderedDictionary Tag_UMEM_Checklist = new OrderedDictionary(10);
        }

        class Read_Memory_Operation
        {
            public static readonly byte Idle = 0x00;
            public static readonly byte Get_EPC = 0x01;
            public static readonly byte Get_UMEM = 0x02;
        }
        

        #region Constructor & Destructor

        Reader_Params Reader_Param = new Reader_Params();

        public RFIdent_Class(byte Read_Type)
        {
            Reader_Param.Read_Type = Read_Type;
            Reader_Param.Tag_EPC_Checklist.Clear();
            Reader_Param.Tag_UMEM_Checklist.Clear();
            tmrConnection_Init();
            tmrDataProcessing_Init(); 
        }

        ~RFIdent_Class()
        {

        }

        #endregion


        #region Custom Exception Class

        class Connection_Exception : Exception
        {
            private string MSG;

            public Connection_Exception(string MSG)
            {
                // TODO: Complete member initialization
                this.MSG = MSG;
            }

            public override string Message
            {
                get
                {
                    return MSG;
                }
            }
        }

        class Processing_Exception : Exception
        {
            private string MSG;
            private byte ErrorCode;

            public Processing_Exception(string MSG, byte ErrorCode)
            {
                // TODO: Complete member initialization
                this.MSG = MSG;
                this.ErrorCode = ErrorCode;
            }

            public override string Message
            {
                get
                {
                    return MSG;
                }
            }

            public byte Error_Code
            {
                get
                {
                    return ErrorCode;
                }
            }
        }

        class Operation_Exception : Exception
        {
            private string MSG;

            public Operation_Exception(string MSG)
            {
                // TODO: Complete member initialization
                this.MSG = MSG;
            }

            public override string Message
            {
                get
                {
                    return MSG;
                }
            }
        }

        #endregion


        #region EPC & UMEM Dummy Data Response Generator
        //F0-10-EE-01-06-45-00-00-00-E2-80-11-05-20-00-57-8C-4B
        //f0:2a:ec:e2:80:11:05:20:00:57:8c:45:7c:41:48:47:31:20:31:38:35:37:31:35:7c:41:41:41:4e:50:52:37:31:4b:44:37:31:30:30:33:39:33:00:d5

        /// <summary>
        /// Generate The Reader Response for Reading 12 byte of EPC data
        /// </summary>
        /// <param name="TID_Offset">TID Offset</param>
        /// <param name="TagType">Type of the tag: B = Body, C = Chassis, E = Engine</param>
        /// <returns>Dummy Response in Byte Array</returns>
        public byte[] Generate_EPC_Dummy(byte TID_Offset, char TagType)
        {
            List<byte> Response = new List<byte>();
            Response.Add(0xF0); //BootCode
            Response.Add(0x10); //Length
            Response.Add(0xEE); //CMD Code
            Response.Add(0x01); //EPC Bank
            Response.Add(0x06); //Word Count
            //Start EPC Data
            Response.Add((byte)TagType); // Tag Type
            Response.Add(0x00); // Reserved
            Response.Add(0x00); // Reserved
            Response.Add(0x00); // Reserved
            Response.Add(0xE2); // TID
            Response.Add(0x80); //
            Response.Add(0x11); //
            Response.Add(0x05); // 
            Response.Add(0x20); //
            Response.Add(0x00); //
            Response.Add(0x57); //
            Response.Add(TID_Offset); //
            //End EPC Data
            Response.Add(CalculateChecksum(Response.ToArray(), 0, Response.Count));
            return Response.ToArray();
        }

        /// <summary>
        /// Generate The Reader Response for Reading 40 byte of UMEM data
        /// </summary>
        /// <param name="TID_Offset">TID Offset</param>
        /// <param name="Pair_Choice">Choice: 0~11. Each choice will generate a different Body/Chassis/Engine No. </param>
        /// <param name="TagType">Type of the tag: B = Body, C = Chassis, E = Engine</param>
        /// <returns>Dummy Response in Byte Array</returns>
        public byte[] Generate_UMEM_Dummy(byte TID_Offset, byte Pair_Choice, char TagType)
        {
            List<byte> Response = new List<byte>();
            Response.Add(0xF0); //BootCode
            Response.Add(0x2A); //Length
            Response.Add(0xEC); //CMD Code
            //Start UMEM Data            
            Response.Add(0xE2); // TID
            Response.Add(0x80); //
            Response.Add(0x11); //
            Response.Add(0x05); // 
            Response.Add(0x20); //
            Response.Add(0x00); //
            Response.Add(0x57); //
            Response.Add(TID_Offset); //

            if (TagType == 'B' || TagType == 'b')
            {
                switch (Pair_Choice % 12)
                {
                    case 0: Response.AddRange(Encoding.ASCII.GetBytes("B|PR71UK-4-1|"));
                        break;
                    case 1: Response.AddRange(Encoding.ASCII.GetBytes("B|PR71UK-4-2|"));
                        break;
                    case 2: Response.AddRange(Encoding.ASCII.GetBytes("B|PR71UK-4-3|"));
                        break;
                    case 3: Response.AddRange(Encoding.ASCII.GetBytes("B|PR71UK-4-4|"));
                        break;
                    case 4: Response.AddRange(Encoding.ASCII.GetBytes("B|PR71UK-4-5|"));
                        break;
                    case 5: Response.AddRange(Encoding.ASCII.GetBytes("B|PR71UK-4-6|"));
                        break;
                    case 6: Response.AddRange(Encoding.ASCII.GetBytes("B|PR71UK-4-7|"));
                        break;
                    case 7: Response.AddRange(Encoding.ASCII.GetBytes("B|PR71UK-4-8|"));
                        break;
                    case 8: Response.AddRange(Encoding.ASCII.GetBytes("B|PR71UK-4-9|"));
                        break;
                    case 9: Response.AddRange(Encoding.ASCII.GetBytes("B|PR71UK-4-10|"));
                        break;
                    case 10: Response.AddRange(Encoding.ASCII.GetBytes("B|PR71UK-4-11|"));
                        break;
                    case 11: Response.AddRange(Encoding.ASCII.GetBytes("B|PR71UK-4-12|"));
                        break;
                }
            }
            else if (TagType == 'C' || TagType == 'c')
            {
                switch (Pair_Choice % 12)
                {
                    case 0: Response.AddRange(Encoding.ASCII.GetBytes("C|JAANPR71KD7100388|4HG1 185710"));
                        break;
                    case 1: Response.AddRange(Encoding.ASCII.GetBytes("C|JAANPR71KD7100389|4HG1 185717"));
                        break;
                    case 2: Response.AddRange(Encoding.ASCII.GetBytes("C|JAANPR71KD7100390|4HG1 185721"));
                        break;
                    case 3: Response.AddRange(Encoding.ASCII.GetBytes("C|JAANPR71KD7100391|4HG1 185726"));
                        break;
                    case 4: Response.AddRange(Encoding.ASCII.GetBytes("C|JAANPR71KD7100392|4HG1 185728"));
                        break;
                    case 5: Response.AddRange(Encoding.ASCII.GetBytes("C|JAANPR71KD7100393|4HG1 185730"));
                        break;
                    case 6: Response.AddRange(Encoding.ASCII.GetBytes("C|JAANPR71KD7100394|4HG1 185741"));
                        break;
                    case 7: Response.AddRange(Encoding.ASCII.GetBytes("C|JAANPR71KD7100395|4HG1 185750"));
                        break;
                    case 8: Response.AddRange(Encoding.ASCII.GetBytes("C|JAANPR71KD7100396|4HG1 185752"));
                        break;
                    case 9: Response.AddRange(Encoding.ASCII.GetBytes("C|JAANPR71KD7100397|4HG1 185757"));
                        break;
                    case 10: Response.AddRange(Encoding.ASCII.GetBytes("C|JAANPR71KD7100398|4HG1 185759"));
                        break;
                    case 11: Response.AddRange(Encoding.ASCII.GetBytes("C|JAANPR71KD7100399|4HG1 185763"));
                        break;
                }
            }
            else if (TagType == 'E' || TagType == 'e')
            {
                switch (Pair_Choice % 12)
                {
                    case 0: Response.AddRange(Encoding.ASCII.GetBytes("E|4HG1 185710|JAANPR71KD7100388"));
                        break;
                    case 1: Response.AddRange(Encoding.ASCII.GetBytes("E|4HG1 185717|JAANPR71KD7100389"));
                        break;
                    case 2: Response.AddRange(Encoding.ASCII.GetBytes("E|4HG1 185721|JAANPR71KD7100390"));
                        break;
                    case 3: Response.AddRange(Encoding.ASCII.GetBytes("E|4HG1 185726|JAANPR71KD7100391"));
                        break;
                    case 4: Response.AddRange(Encoding.ASCII.GetBytes("E|4HG1 185728|JAANPR71KD7100392"));
                        break;
                    case 5: Response.AddRange(Encoding.ASCII.GetBytes("E|4HG1 185730|JAANPR71KD7100393"));
                        break;
                    case 6: Response.AddRange(Encoding.ASCII.GetBytes("E|4HG1 185741|JAANPR71KD7100394"));
                        break;
                    case 7: Response.AddRange(Encoding.ASCII.GetBytes("E|4HG1 185750|JAANPR71KD7100395"));
                        break;
                    case 8: Response.AddRange(Encoding.ASCII.GetBytes("E|4HG1 185752|JAANPR71KD7100396"));
                        break;
                    case 9: Response.AddRange(Encoding.ASCII.GetBytes("E|4HG1 185757|JAANPR71KD7100397"));
                        break;
                    case 10: Response.AddRange(Encoding.ASCII.GetBytes("E|4HG1 185759|JAANPR71KD7100398"));
                        break;
                    case 11: Response.AddRange(Encoding.ASCII.GetBytes("E|4HG1 185763|JAANPR71KD7100399"));
                        break;
                }
            }

            //Fill remaining space with null
            int CMD_Length = Response.Count - 1; //Excluding BootCode

            for(int i = CMD_Length; i<0x2A; i++)
            {
                Response.Add(0x00);
            }

            //End UMEM Data
            Response.Add(CalculateChecksum(Response.ToArray(), 0, Response.Count));
            return Response.ToArray();
        }
        #endregion


        #region 1.Connection Section
        //This Section contain Connectivity Function

        enum Connection_Type
        {
            Serial,
            TCP
        }

        //These Objects Handle the TPC/IP connection
        TcpClient tcpClient;
        Stream TcpStream_Client;
        System.Timers.Timer tmrTcpPolling = new System.Timers.Timer();
        System.Timers.Timer tmrSendCommand = new System.Timers.Timer();


        //A Buffer to hold all incoming Data from the reader
        static readonly object receivedDataList_LOCK = new object();
        List<byte> receivedDataList = new List<byte>();
        ConcurrentQueue<byte[]> CommandQueue = new ConcurrentQueue<byte[]>();
        Connection_Type Con_Type = new Connection_Type();

        private void tmrConnection_Init()
        {
            tmrTcpPolling.Interval = 1;
            tmrTcpPolling.Elapsed += new ElapsedEventHandler(tmrTcpPolling_Elapsed);
            tmrSendCommand.Interval = 400;
            tmrSendCommand.Elapsed += new ElapsedEventHandler(tmrSendCommand_Elapsed);
        }

        /// <summary>
        /// Ping an IP Address
        /// </summary>
        /// <param name="TCP_IP_Address">The IP Address</param>
        /// <returns>True = Ping Successful, False = Ping Fail</returns>
        public bool Connection_Ping(string TCP_IP_Address)
        {
            //Ping the IP address before attempting to make a connection
            //will save your time on waiting for the client to connect to 
            //a non existing ip address
            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();

            // Use the default Ttl value which is 128,
            // but change the fragmentation behavior.
            options.DontFragment = true;

            // Create a buffer of 32 bytes of data to be transmitted.
            string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            int timeout = 120;
            PingReply reply = pingSender.Send(TCP_IP_Address, timeout, buffer, options);

            return (reply.Status == IPStatus.Success);
        }

        /// <summary>
        /// Connect to TM70x using LAN
        /// </summary>
        /// <param name="TCP_IP_Address">TCP/IP Address. E.g. 192.168.0.100</param>
        /// <param name="TCP_Port_Number">TCP Port Number. E.g. 2101</param>
        public bool Connection_Connect(string TCP_IP_Address, int TCP_Port_Number)
        {
            try
            {
                //Ping the IP address before attempting to make a connection
                //will save your time on waiting for the client to connect to 
                //a non existing ip address
                Ping pingSender = new Ping();
                PingOptions options = new PingOptions();

                // Use the default Ttl value which is 128,
                // but change the fragmentation behavior.
                options.DontFragment = true;

                // Create a buffer of 32 bytes of data to be transmitted.
                string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
                byte[] buffer = Encoding.ASCII.GetBytes(data);
                int timeout = 120;
                PingReply reply = pingSender.Send(TCP_IP_Address, timeout, buffer, options);

                if (reply.Status == IPStatus.Success)
                {
                    if (tcpClient != null && tcpClient.Connected == true)
                        tcpClient.Close();

                    tcpClient = new TcpClient();
                    tcpClient.Connect(TCP_IP_Address, TCP_Port_Number);

                    if (tcpClient.Connected)
                    {
                        TcpStream_Client = tcpClient.GetStream();
                        tmrTcpPolling.Start();
                        Con_Type = Connection_Type.TCP;
                        tmrSendCommand.Start();
                        tmrDataProcessing.Start();
                        
                        return true;
                    }
                    else
                    {
                        throw new Connection_Exception("Connect Fail");
                    }
                }
                else
                {
                    throw new Connection_Exception("Ping Fail");
                }
            }
            catch (System.Exception ex)
            {
                //Handle Exeption Here
            }
            return false;
        }

        /// <summary>
        /// Disconnect from TM70x
        /// </summary>
        public bool Connection_Disconnect()
        {
            try
            {
                tmrTcpPolling.Stop();
                tmrSendCommand.Stop();
                tmrDataProcessing.Stop();

                if (TcpStream_Client != null)
                {
                    TcpStream_Client.Close();
                    TcpStream_Client = null;
                }

                if (tcpClient != null)
                {
                    tcpClient.Close();
                    tcpClient = null;
                }

                return true;
            }
            catch (System.Exception ex)
            {
                //Handle Exeption Here
            }

            return false;
        }

        /// <summary>
        /// Send Command to TM70x
        /// </summary>
        /// <param name="CommandData">The command data.</param>
        private bool Connection_SendCommand(byte[] CommandData)
        {
            try
            {
                if (Con_Type == Connection_Type.TCP)
                {
                    if (tcpClient.Connected)
                    {
                        TcpStream_Client.Write(CommandData, 0, CommandData.Length);
                        return true;
                    }

                    throw new Connection_Exception("Cannot Send");
                }
                else
                {
                    throw new Connection_Exception( "Invalid Connection Type");
                }
            }
            catch (System.Exception ex)
            {
                //Handle Exception Here
            }

            return false;
        }        

        /// <summary>
        /// Queue Command to Send to TM70x
        /// </summary>
        /// <param name="CommandData">The command data.</param>
        public bool Connection_QueueCommand(byte[] CommandData)
        {
            try
            {
                CommandQueue.Enqueue(CommandData);
                return true;
            }
            catch (System.Exception ex)
            {
                //Handle Exception Here
            }

            return false;
        }

        /// <summary>
        /// Store all incoming data from the TCP into Buffer
        /// </summary>
        private void tmrTcpPolling_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                int byteToRead = tcpClient.Available;

                if (byteToRead > 0)
                {
                    byte[] DataByte = new byte[byteToRead];

                    TcpStream_Client.Read(DataByte, 0, byteToRead);

                    lock (receivedDataList_LOCK)
                    {
                        receivedDataList.AddRange(DataByte);
                    }
                }
            }
            catch (System.Exception ex)
            {
                //Handle Exception Here
            }
        }

        /// <summary>
        /// Handle the timing of when each command is send
        /// </summary>
        private void tmrSendCommand_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                byte[] CMD;

                if (CommandQueue.TryDequeue(out CMD))
                {
                    Connection_SendCommand(CMD);
                }
            }
            catch (Exception ex)
            {
                //Handle Exception Here
            }
        }
        #endregion


        #region 2.Data Processing

        ConcurrentQueue<byte[]> EPC_Queue = new ConcurrentQueue<byte[]>();
        ConcurrentQueue<byte[]> UMEM_Queue = new ConcurrentQueue<byte[]>();
        ConcurrentQueue<byte> ERROR_Queue = new ConcurrentQueue<byte>();
        System.Timers.Timer tmrDataProcessing = new System.Timers.Timer();

        void tmrDataProcessing_Init()
        {
            tmrDataProcessing.Interval = 1;
            tmrDataProcessing.Elapsed += new ElapsedEventHandler(tmrDataProcessing_Elapsed);
        }

        /// <summary>
        /// Timer For Process All incoming Data From TCP IP Socket
        /// </summary>
        void tmrDataProcessing_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                List<byte> DataBytes = new List<byte>();
                int RemoveCount = 0;
                bool ProcessingDone = false;

                lock (receivedDataList_LOCK)
                {
                    if (receivedDataList.Count > 0)
                        DataBytes.AddRange(receivedDataList.ToArray());
                }

                if (DataBytes.Count >= 4)
                {
                    while (ProcessingDone == false && DataBytes.Count > 0)
                    {
                        if (DataBytes[0] == 0xF0)   //Success Response BootCode
                        {
                            //Check if the Command is valid
                            if (ValidateCommandCode(DataBytes[2]))
                            {
                                int Packet_Length = DataBytes[1] + 2; //Including BootCode & Checksum

                                //Make sure the Response Packet is Complete
                                if (DataBytes.Count >= Packet_Length)
                                {
                                    //F0-10-EE-01-06-45-00-00-00-E2-80-11-05-20-00-57-8C-4B
                                    //f0:2a:ec:e2:80:11:05:20:00:57:8c:45:7c:41:48:47:31:20:31:38:35:37:31:35:7c:41:41:41:4e:50:52:37:31:4b:44:37:31:30:30:33:39:33:00:d5
                                    byte[] Response_Packet = new byte[Packet_Length];
                                    DataBytes.CopyTo(0, Response_Packet, 0, Packet_Length);

                                    //Checksum Validation
                                    byte Calculated_CRC = CalculateChecksum(Response_Packet, 0, Packet_Length - 1);

                                    if (Calculated_CRC == Response_Packet[Packet_Length - 1])
                                    {
                                        //Extract The EPC Data
                                        if (Response_Packet[2] == 0xEE) //List Tag ID
                                        {
                                            int EPC_Length = Response_Packet[4] * 2;
                                            byte[] EPC_Data = new byte[EPC_Length];
                                            DataBytes.CopyTo(5, EPC_Data, 0, EPC_Length);
                                            EPC_Queue.Enqueue(EPC_Data);

                                            //Test
                                            //CommandQueue.Enqueue(Read_Block_UMEM(EPC_Data));
                                        }
                                        else if (Response_Packet[2] == 0xEC) //Read Word Block
                                        {
                                            int UMEM_Length = Packet_Length - 4;
                                            byte[] UMEM_Data = new byte[UMEM_Length];
                                            DataBytes.CopyTo(3, UMEM_Data, 0, UMEM_Length);
                                            UMEM_Queue.Enqueue(UMEM_Data);
                                            //Test
                                            //CommandQueue.Enqueue(List_TagID_EPC());
                                        }

                                        RemoveCount += Packet_Length;
                                        DataBytes.RemoveRange(0, Packet_Length);
                                    }
                                    else
                                    {
                                        RemoveCount++;
                                        DataBytes.RemoveAt(0);
                                    }
                                }
                                else
                                    break;
                            }
                            else   //Remove Invalid Data Byte
                            {
                                RemoveCount++;
                                DataBytes.RemoveAt(0);
                            }
                        }
                        else if (DataBytes[0] == 0xF4)  //Fail Response BootCode
                        {
                            //Check if the Command is valid
                            if (ValidateCommandCode(DataBytes[2]))
                            {
                                int Packet_Length = DataBytes[1] + 2; //Including BootCode & Checksum

                                //Make sure the Response Packet is Complete
                                if (DataBytes.Count >= Packet_Length)
                                {
                                    //F4-03-EE-02-19
                                    byte[] Response_Packet = new byte[Packet_Length];
                                    DataBytes.CopyTo(0, Response_Packet, 0, Packet_Length);

                                    //Checksum Validation
                                    byte Calculated_CRC = CalculateChecksum(Response_Packet, 0, Packet_Length - 1);

                                    if (Calculated_CRC == Response_Packet[Packet_Length - 1])
                                    {
                                        //Handle Fail Response Here
                                        //ERROR_Queue.Enqueue(Response_Packet[3].ToString("X2") + "|" + Parse_ErrorCode(Response_Packet[3]));
                                        ERROR_Queue.Enqueue(Response_Packet[3]);
                                        RemoveCount += Packet_Length;
                                        DataBytes.RemoveRange(0, Packet_Length);
                                    }
                                    else
                                    {
                                        RemoveCount++;
                                        DataBytes.RemoveAt(0);
                                    }
                                }
                                else
                                    break;
                            }
                            else  //Remove Invalid Data Byte
                            {
                                RemoveCount++;
                                DataBytes.RemoveAt(0);
                            }

                            //Exit Loop when no more data to process
                            if (DataBytes.Count == 0)
                                ProcessingDone = true;
                        }
                    }

                    //Remove Processed Data From Main Buffer
                    if (RemoveCount > 0)
                    {
                        lock (receivedDataList_LOCK)
                        {
                            receivedDataList.RemoveRange(0, RemoveCount);
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        /// <summary>
        /// Calculates the 1 Byte checksum.
        /// </summary>
        /// <param name="DataByte">The data byte.</param>
        /// <param name="intOffset">The offset.</param>
        /// <param name="intLength">Length of data byte.</param>
        /// <returns></returns>
        public byte CalculateChecksum(byte[] DataByte, int intOffset, int intLength)
        {
            int intSum = 0, X;
            byte bySum;

            bySum = 0;
            for (X = intOffset; X <= intOffset + intLength - 1; X++)
            {
                intSum += (int)DataByte[X];
            }

            if (intSum < 256)
                bySum = Convert.ToByte(intSum);
            else
                bySum = (byte)(intSum & 255);


            //invert the sum of all bytes (as a byte) and add one
            return (byte)(~bySum + 1);

        }

        /// <summary>
        /// Check if the Command Code is Valid
        /// </summary>
        /// <param name="CommandCode">The command code.</param>
        /// <returns>True = Valid, False = Invalid</returns>
        bool ValidateCommandCode(byte CommandCode)
        {
            bool Valid = false;

            switch (CommandCode)
            {
                case 0x01://Set Baudrate
                    Valid = true;
                    break;
                case 0x02://Get Reader Version
                    Valid = true;
                    break;
                case 0x04://Set Output Power
                    Valid = true;
                    break;
                case 0x05://Set Frequency
                    Valid = true;
                    break;
                case 0x06://Read Param
                    Valid = true;
                    break;
                case 0x09://Set Param
                    Valid = true;
                    break;
                case 0x14://Read Auto Param
                    Valid = true;
                    break;
                case 0x13://Set Auto Param
                    Valid = true;
                    break;
                case 0x0A://Select Antanna
                    Valid = true;
                    break;
                case 0x0E://Reboot
                    Valid = true;
                    break;
                case 0x03://Set Relay
                    Valid = true;
                    break;
                case 0x54://Report Now
                    Valid = true;
                    break;
                case 0x57://Get Tag Info
                    Valid = true;
                    break;
                case 0xEE://List Tag ID
                    Valid = true;
                    break;
                case 0xED://Get ID List
                    Valid = true;
                    break;
                case 0xEC://Read Word Block
                    Valid = true;
                    break;
                case 0xEB://Write Word Block
                    Valid = true;
                    break;
                case 0xEA://Set Lock
                    Valid = true;
                    break;
                case 0xE5://Set EAS
                    Valid = true;
                    break;
                case 0xE4://EAS Alarm
                    Valid = true;
                    break;
                case 0xE3://Read Protect
                    Valid = true;
                    break;
                case 0xE2://RST Read Protect
                    Valid = true;
                    break;
                case 0xE7://Write EPC
                    Valid = true;
                    break;
            }

            return Valid;
        }

        /// <summary>
        /// Parse the Error Code
        /// </summary>
        /// <param name="ErrorCode">The error code.</param>
        /// <returns>Error Message</returns>
        string Parse_ErrorCode(byte ErrorCode)
        {
            switch (ErrorCode)
            {
                case 0x00:
                    return "Success";
                case 0x01:
                    return "Antenna Connection fail";
                case 0x02:
                    return "Detect no Tag";
                case 0x03:
                    return "Illegal tag";
                case 0x04:
                    return "Read write power is inadequate";
                case 0x05:
                    return "Write protection in this area";
                case 0x06:
                    return "Checksum error";
                case 0x07:
                    return "Parameter wrong";
                case 0x08:
                    return "Non-existing data area";
                case 0x09:
                    return "Wrong password";
                case 0x0A:
                    return "Kill password";
                case 0x0B:
                    return "When reader is in Auto-Mode, the command is illegal.";
                case 0x0C:
                    return "Illegal user with unmatched password";
                case 0x0D:
                    return "External RF Interference";
                case 0x0E:
                    return "Read protection on tag";
                case 0x1E:
                    return "Invalid command, such as wrong parameter command";
                case 0x1F:
                    return "Invalid command";
                case 0x20:
                    return "Other error";
                default:
                    return "Unknown Error Code: " + ErrorCode.ToString("X2");
            }
        }

        /// <summary>
        /// Checks if the EPC Queue have any new Data
        /// </summary>
        /// <param name="EPC_Byte">Output the EPC data in Bytes</param>
        /// <returns>True = New Data Available, False = No New Data Available</returns>
        public bool CheckEPC_Queue(out byte[] EPC_Byte)
        {
            return EPC_Queue.TryDequeue(out EPC_Byte);
        }

        /// <summary>
        /// Checks if the UMEM Queue have any new Data
        /// </summary>
        /// <param name="EPC_Byte">Output the UMEM data in Bytes</param>
        /// <returns>True = New Data Available, False = No New Data Available</returns>
        public bool CheckUMEM_Queue(out byte[] UMEM_Byte)
        {
            return UMEM_Queue.TryDequeue(out UMEM_Byte);
        }

        /// <summary>
        /// Checks if the ERROR Queue have any new Message
        /// </summary>
        /// <param name="EPC_Byte">Output the ERROR Message in String</param>
        /// <returns>True = New Message Available, False = No New Message Available</returns>
        public bool CheckERROR_Queue(out string ERROR_MSG)
        {
            byte ErrorCode;
            bool DataAvailable = ERROR_Queue.TryDequeue(out ErrorCode);
            ERROR_MSG = Parse_ErrorCode(ErrorCode);

            return DataAvailable;
        }

        #endregion
                

        #region 3.Tag Command

        /// <summary>
        /// Generate List Tag ID - EPC Command
        /// </summary>
        /// <returns>Command Bytes</returns>
        public byte[] List_TagID_EPC()
        {
            byte[] CMD = new byte[9];
            CMD[0] = 0x40;      // Header
            CMD[1] = 0x07;      // Length
            CMD[2] = 0xEE;      // List Tag ID CMD Code
            CMD[3] = 0x01;      // EPC
            CMD[4] = 0x00;      // Address
            CMD[5] = 0x00;      // Address
            CMD[6] = 0x00;      // Length
            CMD[7] = 0x00;      // Dummy Data

            CMD[8] = CalculateChecksum(CMD, 0, 8);
            return CMD;
        }

        /// <summary>
        /// Generate List Tag ID - TID Command
        /// </summary>
        /// <returns>Command Bytes</returns>
        public byte[] List_TagID_TID()
        {
            byte[] CMD = new byte[9];
            CMD[0] = 0x40;      // Header
            CMD[1] = 0x07;      // Length
            CMD[2] = 0xEE;      // List Tag ID CMD Code
            CMD[3] = 0x02;      // TID
            CMD[4] = 0x00;      // Address
            CMD[5] = 0x00;      // Address
            CMD[6] = 0x00;      // Length
            CMD[7] = 0x00;      // Dummy Data

            CMD[8] = CalculateChecksum(CMD, 0, 8);
            return CMD;
        }

        /// <summary>
        /// Generate Read User Memory Block Command
        /// </summary>
        /// <returns>Command Bytes</returns>
        public byte[] Read_Block_UMEM(byte[] EPC)
        {
            //40:16:ec:06:45:00:00:00:e2:80:11:05:20:00:57:8c:03:00:14:00:00:00:00:e1

            List<byte> CMD_List = new List<byte>();
            CMD_List.Add(0x40);// Header
            CMD_List.Add((byte)(0x06 + 4 + EPC.Length));// Length
            CMD_List.Add(0xEC);// List Tag ID CMD Code
            CMD_List.Add((byte)(EPC.Length/2)); //EPC Word Count
            CMD_List.AddRange(EPC); //EPC Data
            CMD_List.Add(0x03); //UMEM Bank
            CMD_List.Add(0x00); //Address
            CMD_List.Add(0x14); //Length
            CMD_List.Add(0x00); // Password
            CMD_List.Add(0x00);
            CMD_List.Add(0x00);
            CMD_List.Add(0x00);

            CMD_List.Add(CalculateChecksum(CMD_List.ToArray(), 0, CMD_List.Count));
            return CMD_List.ToArray();
        }

        #endregion


        #region 4.Operation

        /// <summary>
        /// Check if the EPC data already exist
        /// </summary>
        /// <param name="EPC_Bytes">The EPC Data Bytes.</param>
        /// <returns>True = Exist, False = New Record.</returns>
        private bool Check_Tag_EPC_Checklist(byte[] EPC_Bytes)
        {
            if (Reader_Param.Tag_EPC_Checklist.Contains(EPC_Bytes))
            {
                return true;
            }
            else
            {
                if (Reader_Param.Tag_EPC_Checklist.Count >= 10)
                    Reader_Param.Tag_EPC_Checklist.RemoveAt(0);

                Reader_Param.Tag_EPC_Checklist.Add(EPC_Bytes, 0);
                
                return false;
            }
        }

        /// <summary>
        /// Check if the UMEM data already exist
        /// </summary>
        /// <param name="EPC_Bytes">The UMEM Data Bytes.</param>
        /// <returns>True = Exist, False = New Record.</returns>
        private bool Check_Tag_UMEM_Checklist(byte[] UMEM_Bytes)
        {
            if (Reader_Param.Tag_UMEM_Checklist.Contains(UMEM_Bytes))
            {
                return true;
            }
            else
            {
                if (Reader_Param.Tag_UMEM_Checklist.Count >= 10)
                    Reader_Param.Tag_UMEM_Checklist.RemoveAt(0);

                Reader_Param.Tag_UMEM_Checklist.Add(UMEM_Bytes, 0);

                return false;
            }
        }


        /// <summary>
        /// Get List Of Tag
        /// </summary>
        /// <param name="Complete">CallBack Function</param>
        /// <exception cref="SensMaster.RFIdent_Class.Operation_Exception">
        /// Chassis & Engine Mismatch!
        /// or
        /// or
        /// Operation Timeout
        /// </exception>
        public void Get_List_Of_Tag(Func<Tag[], bool> Complete)
        {
            try
            {
                int OperationTimeOut = 500;
                bool Done = false;
                List<Tag> Tag_List = new List<Tag>();
                byte[] EPC_Bytes;
                byte[] UMEM_Bytes;
                byte ERROR_Code;
                OrderedDictionary Marriage_EPC_CheckList = new OrderedDictionary(3);
                OrderedDictionary Marriage_UMEM_CheckList = new OrderedDictionary(3);

                Reader_Param.Current_Read_Memory_OP = Read_Memory_Operation.Get_EPC;

                //Start Reading Tag
                Connection_SendCommand(List_TagID_EPC());
                
                while (!Done)
                {
                    //Enter here when Single Tag Read Type
                    if (Reader_Param.Read_Type == 0x01)   //Single Tag
                    {
                        #region Single Tag operation
                        //Get Tag EPC Data Operation
                        if (Reader_Param.Current_Read_Memory_OP == Read_Memory_Operation.Get_EPC)
                        {
                            //Check if there is any new EPC data
                            if (EPC_Queue.TryDequeue(out EPC_Bytes))
                            {
                                //Check If Tag EPC already been read before
                                if (!Check_Tag_EPC_Checklist(EPC_Bytes))
                                {
                                    Connection_SendCommand(Read_Block_UMEM(EPC_Bytes));
                                    Reader_Param.Current_Read_Memory_OP = Read_Memory_Operation.Get_UMEM;
                                }
                                else
                                {
                                    //Ignore already read tag
                                }
                            }
                            //Check if error is returned
                            else if (ERROR_Queue.TryDequeue(out ERROR_Code))
                            {
                                //When these error code is recived resend List TagID Command otherwise throw exception
                                if (ERROR_Code == 0x02 || ERROR_Code == 0x07 || ERROR_Code == 0x08) // Detect no Tag || Parameter wrong || Non-existing data area
                                    Connection_SendCommand(List_TagID_EPC());
                                else
                                    throw new Operation_Exception(Parse_ErrorCode(ERROR_Code));
                            }
                            else
                            {
                                OperationTimeOut--;
                                Thread.Sleep(10);
                            }
                        }
                        else if (Reader_Param.Current_Read_Memory_OP == Read_Memory_Operation.Get_UMEM)
                        {
                            //Check if there is any new UMEM data
                            if (UMEM_Queue.TryDequeue(out UMEM_Bytes))
                            { 
                                //Extract TID
                                byte[] TID = new byte[8];
                                Array.Copy(UMEM_Bytes, 0, TID, 0, 8);  

                                //Extract Tagtype & Body/Chassis/Engine No.
                                string UMEM_String = Encoding.ASCII.GetString(UMEM_Bytes, 8, 32);
                                string[] UMEM_Split = UMEM_String.Split('|');

                                Tag CurrentTag = new Tag(TID);

                                if (UMEM_Split[0] == "B")   //Body Tag
                                {
                                    CurrentTag.PunchBody = UMEM_Split[1];
                                    CurrentTag.ChassisNo = "";
                                    CurrentTag.EngineNo = "";
                                }
                                else if (UMEM_Split[0] == "C")  // Chassis Tag
                                {
                                    CurrentTag.PunchBody = "";
                                    CurrentTag.ChassisNo = UMEM_Split[1];
                                    CurrentTag.EngineNo = UMEM_Split[2];
                                }
                                else if (UMEM_Split[0] == "E") //Engine Tag
                                {
                                    CurrentTag.PunchBody = "";
                                    CurrentTag.ChassisNo = UMEM_Split[2];
                                    CurrentTag.EngineNo = UMEM_Split[1];
                                }

                                Tag_List.Add(CurrentTag);
                                Complete(Tag_List.ToArray());
                                Done = true;
                            }
                            //Check if error is return
                            else if (ERROR_Queue.TryDequeue(out ERROR_Code))
                            {
                                //When these error code is received resend Read User Memory Block Command otherwise throw exception
                                if (ERROR_Code == 0x02 || ERROR_Code == 0x07 || ERROR_Code == 0x08) // Detect no Tag || Parameter wrong || Non-existing data area
                                    Connection_SendCommand(Read_Block_UMEM((byte[])Reader_Param.Tag_EPC_Checklist[Reader_Param.Tag_EPC_Checklist.Count-1]));
                                else
                                    throw new Operation_Exception(Parse_ErrorCode(ERROR_Code));
                            }
                            else
                            {
                                OperationTimeOut--;
                                Thread.Sleep(10);
                            }
                        }

                        #endregion Single Tag operation
                    }
                    else if (Reader_Param.Read_Type == 0x03)   //Marriage Tag
                    {
                        #region Marriage Tag operation
                        //Get Tag EPC Data Operation
                        if (Reader_Param.Current_Read_Memory_OP == Read_Memory_Operation.Get_EPC)
                        {
                            //Check if there is any new EPC data
                            if (EPC_Queue.TryDequeue(out EPC_Bytes))
                            {
                                //Check If Tag EPC already been read before
                                if (!Check_Tag_EPC_Checklist(EPC_Bytes))
                                {
                                    if (!Marriage_EPC_CheckList.Contains(EPC_Bytes[0]))
                                    {
                                        Marriage_EPC_CheckList.Add(EPC_Bytes[0], EPC_Bytes);
                                    }
                                }
                                else
                                {
                                    //Ignore already read tag
                                }

                                if (Marriage_EPC_CheckList.Count == 3)
                                {
                                    Connection_SendCommand(Read_Block_UMEM((byte[])Marriage_EPC_CheckList[0]));
                                    Reader_Param.Current_Read_Memory_OP = Read_Memory_Operation.Get_UMEM;
                                }
                            }
                            //Check if error is returned
                            else if (ERROR_Queue.TryDequeue(out ERROR_Code))
                            {
                                //When these error code is recived resend List TagID Command otherwise throw exception
                                if (ERROR_Code == 0x02 || ERROR_Code == 0x07 || ERROR_Code == 0x08) // Detect no Tag || Parameter wrong || Non-existing data area
                                    Connection_SendCommand(List_TagID_EPC());
                                else
                                    throw new Operation_Exception(Parse_ErrorCode(ERROR_Code));
                            }
                            else
                            {
                                OperationTimeOut--;
                                Thread.Sleep(10);
                            }
                        }
                        else if (Reader_Param.Current_Read_Memory_OP == Read_Memory_Operation.Get_UMEM)
                        {
                            //Check if there is any new UMEM data
                            if (UMEM_Queue.TryDequeue(out UMEM_Bytes))
                            {
                                if (!Check_Tag_UMEM_Checklist(UMEM_Bytes))
                                {
                                    if (!Marriage_UMEM_CheckList.Contains(UMEM_Bytes[8]))
                                    {
                                        Marriage_UMEM_CheckList.Add((char)UMEM_Bytes[8], UMEM_Bytes);
                                    }

                                    Marriage_EPC_CheckList.RemoveAt(0);

                                    if (Marriage_EPC_CheckList.Count != 0)
                                    {
                                        Connection_SendCommand(Read_Block_UMEM((byte[])Marriage_EPC_CheckList[0]));
                                    }

                                    if (Marriage_UMEM_CheckList.Count == 3)
                                    {
                                        //Extract TID, Body/Chassis/Engine No.
                                        byte[] BodyTag_UMEM_Byte = (byte[])Marriage_UMEM_CheckList[(object)'B'];
                                        byte[] ChassisTag_UMEM_Byte = (byte[])Marriage_UMEM_CheckList[(object)'C'];
                                        byte[] EngineTag_UMEM_Byte = (byte[])Marriage_UMEM_CheckList[(object)'E'];
                                        string BodyTag_UMEM_String = Encoding.ASCII.GetString(BodyTag_UMEM_Byte, 8, 32);
                                        string ChassisTag_UMEM_String = Encoding.ASCII.GetString(ChassisTag_UMEM_Byte, 8, 32);
                                        string EngineTag_UMEM_String = Encoding.ASCII.GetString(EngineTag_UMEM_Byte, 8, 32);
                                        string[] BodyTag_UMEM_Split = BodyTag_UMEM_String.Split('|');
                                        string[] ChassisTag_UMEM_Split = ChassisTag_UMEM_String.Split('|');
                                        string[] EngineTag_UMEM_Split = EngineTag_UMEM_String.Split('|');
                                        byte[] BodyTag_TID = new byte[8];
                                        byte[] ChassisTag_TID = new byte[8];
                                        byte[] EngineTag_TID = new byte[8];

                                        Array.Copy(BodyTag_UMEM_Byte, 0, BodyTag_TID, 0, 8);
                                        Array.Copy(ChassisTag_UMEM_Byte, 0, ChassisTag_TID, 0, 8);
                                        Array.Copy(EngineTag_UMEM_Byte, 0, EngineTag_TID, 0, 8);

                                        if (ChassisTag_UMEM_Split[1].Replace("\0", "") == EngineTag_UMEM_Split[2].Replace("\0", "") && 
                                            ChassisTag_UMEM_Split[2].Replace("\0", "") == EngineTag_UMEM_Split[1].Replace("\0", ""))
                                        {
                                            Body BodyTag = new Body(BodyTag_TID, BodyTag_UMEM_Split[1]);
                                            Chassis ChassisTag = new Chassis(ChassisTag_TID,EngineTag_UMEM_Split[1],EngineTag_UMEM_Split[2]);
                                            Engine EngineTag = new Engine(EngineTag_TID,EngineTag_UMEM_Split[1],EngineTag_UMEM_Split[2]);
                                            Tag_List.AddRange(new Tag[]{BodyTag, ChassisTag, EngineTag});
                                            Complete(Tag_List.ToArray());
                                            Done = true;
                                        }
                                        else
                                        {
                                            throw new Operation_Exception("Chassis & Engine Mismatch!");
                                        }
                                    }
                                }
                            }
                            //Check if error is return
                            else if (ERROR_Queue.TryDequeue(out ERROR_Code))
                            {
                                //When these error code is received resend Read User Memory Block Command otherwise throw exception
                                if (ERROR_Code == 0x02 || ERROR_Code == 0x07 || ERROR_Code == 0x08) // Detect no Tag || Parameter wrong || Non-existing data area
                                    Connection_SendCommand(Read_Block_UMEM((byte[])Marriage_EPC_CheckList[0]));
                                else
                                    throw new Operation_Exception(Parse_ErrorCode(ERROR_Code));
                            }
                            else
                            {
                                OperationTimeOut--;
                                Thread.Sleep(10);
                            }
                        }

                        #endregion Marriage Tag operation
                    }

                    if (OperationTimeOut <= 0)
                        throw new Operation_Exception("Operation Timeout");
                }
            }
            catch (Exception ex)
            {
                //Handle Exception Here
            }
        }

        /// <summary>
        /// Test Function For "Get List Of Tag" Function(No Physical Reader Required).
        /// </summary>
        /// <param name="Complete">CallBack Function</param>
        /// <exception cref="SensMaster.RFIdent_Class.Operation_Exception">
        /// Chassis & Engine Mismatch!
        /// or
        /// or
        /// Operation Timeout
        /// </exception>
        public void Get_List_Of_Tag_Test(Func<Tag[], bool> Complete)
        {
            try
            {
                Random ran = new Random();
                

                tmrDataProcessing.Start();
                lock (receivedDataList_LOCK)
                {
                    if (Reader_Param.Read_Type == 0x01) 
                    {
                        byte TID = (byte)ran.Next(0, 11);
                        byte Pair_Choice= (byte)ran.Next(0, 11);
                        byte TagType = (byte)ran.Next(0, 2);
                        //Dummy Data For Single Tag
                        switch (TagType)
                        {
                            case 0:
                                receivedDataList.AddRange(Generate_EPC_Dummy(TID, 'B'));
                                receivedDataList.AddRange(Generate_UMEM_Dummy(TID, Pair_Choice, 'B'));
                                break;
                            case 1:
                                receivedDataList.AddRange(Generate_EPC_Dummy(TID, 'C'));
                                receivedDataList.AddRange(Generate_UMEM_Dummy(TID, Pair_Choice, 'C'));
                                break;
                            case 2:
                                receivedDataList.AddRange(Generate_EPC_Dummy(TID, 'E'));
                                receivedDataList.AddRange(Generate_UMEM_Dummy(TID, Pair_Choice, 'E'));
                                break;
                        }
                        
                    }
                    else
                    {
                        byte B_TID = (byte)ran.Next(0, 11);
                        byte C_TID = (byte)ran.Next(0, 11);
                        byte E_TID = (byte)ran.Next(0, 11);
                        byte Pair_Choice = (byte)ran.Next(0, 11);

                        //Dummy Data For Marriage Tag
                        receivedDataList.AddRange(Generate_EPC_Dummy(B_TID, 'B'));
                        receivedDataList.AddRange(Generate_EPC_Dummy(C_TID, 'C'));
                        receivedDataList.AddRange(Generate_EPC_Dummy(E_TID, 'E'));
                        receivedDataList.AddRange(Generate_UMEM_Dummy(B_TID, Pair_Choice, 'B'));
                        //Matching Chassis & Engine No.
                        receivedDataList.AddRange(Generate_UMEM_Dummy(C_TID, Pair_Choice, 'C'));
                        receivedDataList.AddRange(Generate_UMEM_Dummy(E_TID, Pair_Choice, 'E'));
                        //Missmatch Chassis & Engine No.
                        //receivedDataList.AddRange(Generate_UMEM_Dummy(C_TID, Pair_Choice, 'C'));
                        //receivedDataList.AddRange(Generate_UMEM_Dummy(E_TID, Pair_Choice+1, 'E'));
                    }
                }

                Get_List_Of_Tag(Complete);
            }
            catch (Exception ex)
            {
                //Handle Exception Here
            }
        }
        #endregion
    }
}
