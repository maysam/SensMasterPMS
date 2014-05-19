using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SensMaster
{
    public class Reader
    {
        // Read Type Code
        public static readonly byte SINGLETAG = 0x01;
        public static readonly byte MARRIAGETAG = 0x03;

        // Connection Status Code
        public static readonly byte IDLE = 0x00;
        public static readonly byte OK = 0x01;
        public static readonly byte PINGFAIL = 0x02;
        public static readonly byte NOTRESPONDING = 0x03;

        // Current OP Code
        public static readonly byte STOPSEARCH = 0x00;
        public static readonly byte STARTSEARCH = 0x01;
        public static readonly byte DFU = 0x02;

        private RFIdent_Class RF_Reader;
        private string TCP_IP_Address;
        private int TCP_Port;
        private string Location_Name;
        private byte Read_Type;

        public Reader(string TCP_IP_Address, int TCP_Port, string Location_Name, byte Read_Type)
        {
            RF_Reader = new RFIdent_Class(Read_Type);
            this.TCP_IP_Address = TCP_IP_Address;
            this.TCP_Port = TCP_Port;
            this.Location_Name = Location_Name;
            this.Read_Type = Read_Type;
        }

        public void Poll(Func<Tag[], bool> Display)
        {
            RF_Reader.Get_List_Of_Tag_Test(Display);
        }

        public Tag ReadWordBlock(byte[] TagID)
        {
            byte[] read_data = new byte[] { 0x01 };
            return ParseUserMemory(read_data);
        }

        public Tag ParseUserMemory(byte[] read_data)
        {
            byte[] id = new byte[8];
            string tempNumber = "", Number1 = null, Number2 = null;

            Buffer.BlockCopy(read_data, 0, id, 0, 8);
            for (int i = 0; i < 40; i++)
            {
                if (i < 8)
                {
                    id[i] = read_data[i];
                }
                else if (i == 9)
                {
                    if (read_data[i] != 0x7C)
                    {
                        throw new Exception("Invalid Tag(10)");
                    }
                }
                else if (i > 9)
                {
                    if (read_data[i] == 0x00)
                    {
                        break;
                    }
                    else
                        if (read_data[i] == 0x7C)
                        {
                            Number1 = tempNumber;
                            tempNumber = "";
                        }
                        else
                        {
                            tempNumber += (char)read_data[i];
                        }
                }
            }
            Number2 = tempNumber;
            switch ((char)read_data[8])
            {
                case 'E':
                    return new Engine(read_data, Number1, Number2);
                case 'C':
                    return new Chassis(read_data, Number1, Number2);
                case 'B':
                    return new Body(read_data, Number1);
                default:
                    throw new Exception("Invalid Tag");
            }
        }


    }
}