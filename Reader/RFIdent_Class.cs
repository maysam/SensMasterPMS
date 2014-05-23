using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Concurrent;
using System.Timers;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Collections.Specialized;
using System.Threading;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace SensMaster
{
    /// <summary>
    /// 
    /// </summary>
    class RFIdentReader : ReaderDevice
    {
        private Read_Memory_Operation Current_Read_Memory_OP = Read_Memory_Operation.Idle;
        private HashSet<byte[]> Tag_EPC_Checklist = new HashSet<byte[]>();
        private OrderedDictionary Tag_UMEM_Checklist = new OrderedDictionary(10);
      
        enum Read_Memory_Operation : byte
        {
            Idle = 0x00,
            Get_EPC = 0x01,
            Get_UMEM = 0x02
        }
        

        #region Constructor & Destructor

        Reader reader;

        public RFIdentReader(Reader reader, TcpClient client)
        {
            this.reader = reader;
            Tag_EPC_Checklist.Clear();
            Tag_UMEM_Checklist.Clear();
            tmrConnection_Init();
            tmrDataProcessing_Init();
            tcpClient = client;
        }

        #endregion


        #region Custom Exception Class

        class ConnectionException : Exception { }
        class PingException : Exception { }
        class OperationException : Exception {
            public OperationException(string p) : base(p)
            {
            }
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
        System.Timers.Timer tmrTcpPolling;


        //A Buffer to hold all incoming Data from the reader
        static readonly object receivedDataList_LOCK = new object();
        List<byte> receivedDataList = new List<byte>();
        Connection_Type Con_Type = Connection_Type.TCP;

        private void tmrConnection_Init()
        {
            tmrTcpPolling = new System.Timers.Timer(1);
            tmrTcpPolling.Elapsed += new ElapsedEventHandler(tmrTcpPolling_Elapsed);
        }

        /// <summary>
        /// Ping an IP Address
        /// </summary>
        /// <param name="TCP_IP_Address">The IP Address</param>
        /// <returns>True = Ping Successful, False = Ping Fail</returns>
        public bool ping()
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
            PingReply reply = pingSender.Send(reader.IP, timeout, buffer, options);

            return (reply.Status == IPStatus.Success);
        }

        /// <summary>
        /// Connect to TM70x using LAN
        /// </summary>
        /// <param name="TCP_IP_Address">TCP/IP Address. E.g. 192.168.0.100</param>
        /// <param name="TCP_Port_Number">TCP Port Number. E.g. 2101</param>
        public void connect()
        {
            if (ping())
            {
                if (!tcpClient.Connected)
                {
                    try
                    {
                        reader.connect(tcpClient);

                        if (tcpClient.Connected)
                        {
                            // with the exception handling, we probably don't need to check connected
                            TcpStream_Client = tcpClient.GetStream();
                            tmrTcpPolling.Start();
                            tmrDataProcessing.Start();
                        }
                        else
                        {
                            reader.connection_failing();
                        }
                    }
                    catch (Exception ex)
                    {
                        reader.connection_failing(ex);
                    }
                }
            }
            else
            {
                reader.connection_failing(new TimeoutException("Ping Timeout"));
            }
        }

        /// <summary>
        /// Disconnect from TM70x
        /// </summary>
        public bool disconnect()
        {
            try
            {
                tmrTcpPolling.Stop();
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
            catch (Exception ex)
            {
                reader.connection_failing(ex);
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
                    if (!tcpClient.Connected)
                    {
                        reader.connect(tcpClient);
                    }
                    if (tcpClient.Connected)
                    {
                        Thread.Sleep(500);
                        TcpStream_Client.Write(CommandData, 0, CommandData.Length);
                        return true;
                    }

                    throw new ConnectionException();
                }
                else
                {
//                    throw new NotImplementedException("Serial Connection is not implemented!");
                }
            }
            catch (Exception ex)
            {
                reader.connection_failing(ex);
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
            catch (Exception ex)
            {
                reader.connection_failing(ex);
            }
        }
        #endregion


        #region 2.Data Processing

        ConcurrentQueue<byte[]> EPC_Queue = new ConcurrentQueue<byte[]>();
        ConcurrentQueue<byte[]> UMEM_Queue = new ConcurrentQueue<byte[]>();
        ConcurrentQueue<byte> ERROR_Queue = new ConcurrentQueue<byte>();
        System.Timers.Timer tmrDataProcessing;

        void tmrDataProcessing_Init()
        {
            tmrDataProcessing = new System.Timers.Timer(1);
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
                            if (receivedDataList.Count >= RemoveCount)
                                receivedDataList.RemoveRange(0, RemoveCount);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                reader.connection_failing(ex);
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
        /// Check if the UMEM data already exist
        /// </summary>
        /// <param name="EPC_Bytes">The UMEM Data Bytes.</param>
        /// <returns>True = Exist, False = New Record.</returns>
        private bool Check_Tag_UMEM_Checklist(byte[] UMEM_Bytes)
        {
            if (Tag_UMEM_Checklist.Contains(UMEM_Bytes))
            {
                return true;
            }
            else
            {
                if (Tag_UMEM_Checklist.Count >= 10)
                    Tag_UMEM_Checklist.RemoveAt(0);

                Tag_UMEM_Checklist.Add(UMEM_Bytes, 0);

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
        bool in_operation = false;
        public void Get_List_Of_Tag(Action<Tag[], Action> Complete, Action PostComplete)
        {
            if(!in_operation)
                try
                {
                    in_operation = true;
                    int OperationTimeOut = 120; // *60 * 100;
                    OperationTimeOut = 5000;
                    bool Done = false;
                    List<Tag> Tag_List = new List<Tag>();
                    byte[] EPC_Bytes = null;
                    byte[] UMEM_Bytes;
                    byte ERROR_Code;
                    Dictionary<string, byte[]> Marriage_UMEM_CheckList = new Dictionary<string, byte[]>();

                    Current_Read_Memory_OP = Read_Memory_Operation.Get_EPC;

                    //Start Reading Tag
                    Connection_SendCommand(List_TagID_EPC());

                    while (!Done && (OperationTimeOut > 0))
                    {
                        //Enter here when Single Tag Read Type
                        if (reader.Read_Type == ReaderType.SINGLETAG)   //Single Tag
                        {
                            #region Single Tag operation
                            //Get Tag EPC Data Operation
                            if (Current_Read_Memory_OP == Read_Memory_Operation.Get_EPC)
                            {
                                //Check if there is any new EPC data
                                if (EPC_Queue.TryDequeue(out EPC_Bytes))
                                {
                                    //Check If Tag EPC already been read before
                                    if (!Tag_EPC_Checklist.Contains(EPC_Bytes))
                                    {
                                        Tag_EPC_Checklist.Add(EPC_Bytes);
                                        Connection_SendCommand(Read_Block_UMEM(EPC_Bytes));
                                        Current_Read_Memory_OP = Read_Memory_Operation.Get_UMEM;
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
                                    if (ERROR_Code == 0x02 || ERROR_Code == 0x07 || ERROR_Code == 0x08)
                                    {
                                        // Detect no Tag || Parameter wrong || Non-existing data area
                                        //Thread.Sleep(400);
                                        Connection_SendCommand(List_TagID_EPC());
                                    }
                                    else
                                        throw new OperationException(Parse_ErrorCode(ERROR_Code));
                                }
                                else
                                {
                                    OperationTimeOut--;
                                    Thread.Sleep(10);
                                }
                            }
                            else if (Current_Read_Memory_OP == Read_Memory_Operation.Get_UMEM)
                            {
                                //Check if there is any new UMEM data
                                if (UMEM_Queue.TryDequeue(out UMEM_Bytes))
                                {
                                    Tag CurrentTag = ParseUserMemory(UMEM_Bytes);
                                    Complete(new Tag[]{ CurrentTag}, PostComplete);
                                    Done = true;
                                }
                                //Check if error is return
                                else if (ERROR_Queue.TryDequeue(out ERROR_Code))
                                {
                                    //When these error code is received resend Read User Memory Block Command otherwise throw exception
                                    if (ERROR_Code == 0x02 || ERROR_Code == 0x07 || ERROR_Code == 0x08)
                                    {
                                        // Detect no Tag || Parameter wrong || Non-existing data area
                                        //Thread.Sleep(400);
                                        Connection_SendCommand(Read_Block_UMEM(EPC_Bytes));
                                    }
                                    else
                                    {
                                        throw new OperationException(Parse_ErrorCode(ERROR_Code));
                                    }
                                }
                                else
                                {
                                    OperationTimeOut--;
                                    Thread.Sleep(10);
                                }
                            }

                            #endregion Single Tag operation
                        }
                        else if (reader.Read_Type == ReaderType.MARRIAGETAG)   //Marriage Tag
                        {
                            #region Marriage Tag operation
                            //Get Tag EPC Data Operation
                            if (Current_Read_Memory_OP == Read_Memory_Operation.Get_EPC)
                            {
                                //Check if there is any new EPC data
                                if (EPC_Queue.TryDequeue(out EPC_Bytes))
                                {

                                    //Check If Tag EPC already been read before
                                    if (!Marriage_UMEM_CheckList.ContainsKey(BitConverter.ToString(EPC_Bytes)))
                                    {
                                        // read epc's usermem
                                        Connection_SendCommand(Read_Block_UMEM(EPC_Bytes));
                                        Current_Read_Memory_OP = Read_Memory_Operation.Get_UMEM;
                                    }
                                    else
                                    {
                                        // read another epc
                                        Connection_SendCommand(List_TagID_EPC());
                                    }
                                }
                                //Check if error is returned
                                else if (ERROR_Queue.TryDequeue(out ERROR_Code))
                                {
                                    //When these error code is recived resend List TagID Command otherwise throw exception
                                    if (ERROR_Code == 0x02 || ERROR_Code == 0x07 || ERROR_Code == 0x08)
                                    {
                                        // Detect no Tag || Parameter wrong || Non-existing data area
                                        //Thread.Sleep(400);
                                        Connection_SendCommand(List_TagID_EPC());
                                    }
                                    else
                                        throw new OperationException(Parse_ErrorCode(ERROR_Code));
                                }
                                else
                                {
                                    OperationTimeOut--;
                                    Thread.Sleep(10);
                                }
                            }
                            else if (Current_Read_Memory_OP == Read_Memory_Operation.Get_UMEM)
                            {
                                //Check if there is any new UMEM data
                                if (UMEM_Queue.TryDequeue(out UMEM_Bytes))
                                {
                                    if (!Marriage_UMEM_CheckList.ContainsKey(BitConverter.ToString( EPC_Bytes)))
                                    {

                                        Marriage_UMEM_CheckList.Add(BitConverter.ToString(EPC_Bytes), UMEM_Bytes);
                                        Tag_List.Add(ParseUserMemory(UMEM_Bytes));
                                        if (Tag_List.Count == 3)
                                        {
                                            Complete(Tag_List.GetRange(0, 3).ToArray(), PostComplete);
                                            Done = true;
                                        }
                                        else
                                        {
                                            //read next epc
                                            Connection_SendCommand(List_TagID_EPC());
                                            Current_Read_Memory_OP = Read_Memory_Operation.Get_EPC;
                                        }
                                    }
                                }
                                //Check if error is return
                                else if (ERROR_Queue.TryDequeue(out ERROR_Code))
                                {
                                    //When these error code is received resend Read User Memory Block Command otherwise throw exception
                                    if (ERROR_Code == 0x02 || ERROR_Code == 0x07 || ERROR_Code == 0x08) // Detect no Tag || Parameter wrong || Non-existing data area
                                        Connection_SendCommand(Read_Block_UMEM(EPC_Bytes));
                                    else
                                        throw new OperationException(Parse_ErrorCode(ERROR_Code));
                                }
                                else
                                {
                                    OperationTimeOut--;
                                    Thread.Sleep(10);
                                }
                            }

                            #endregion Marriage Tag operation
                        }
                    }
                    if (reader.Read_Type == ReaderType.MARRIAGETAG && !Done)
                    {
                        // didn't get the right tags, call sp_sm_marriage_error_Tag
                    }
                }
                catch (Exception ex)
                {
                    reader.connection_failing(ex);
                }
                finally
                {
                    in_operation = false;
                }
        }


        public Tag ParseUserMemory(byte[] raw_data)
        {
            string raw_data_string = BitConverter.ToString(raw_data);
            string tag_id = BitConverter.ToString(raw_data,0,8);
            string tempNumber = "", Number1 = null, Number2 = null;
            for (int i = 9; i < 40; i++)
            {
                string current_bit = BitConverter.ToString(raw_data, i, 1);
                if (i == 9)
                {
                    if (raw_data[i] != 0x7C)
                    {
                        throw new Exception("Invalid Tag(10)");
                    }
                }
                else if (i > 9)
                {
                    if (raw_data[i] == 0x7C)
                    {
                        Number1 = tempNumber;
                        tempNumber = "";
                    }
                    else
                    {
                        tempNumber += current_bit;
                    }
                }
            }
            Number2 = tempNumber;
            switch ((char)raw_data[8])
            {
                case 'E':
                    return new Engine(reader, raw_data_string, tag_id, Number1, Number2);
                case 'C':
                    return new Chassis(reader, raw_data_string, tag_id, Number1, Number2);
                case 'B':
                    return new Body(reader, raw_data_string, tag_id, Number1);
                default:
                    throw new Exception("Invalid Tag");
            }
        }

        #endregion
    }
}
