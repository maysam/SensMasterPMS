using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;

namespace SensMaster
{
    public class Tag
    {
        public string PunchBody;
        public string ChassisNo;
        public string EngineNo;
        public byte[] ID;

        public Tag(byte[] TagID)
        {
            ID = TagID;
        }
    }

    public class Chassis : Tag
    {
        public Chassis(byte[] TagID, string Chassis_Number, string Engine_Number)
            : base(TagID)
        {
            ChassisNo = Chassis_Number;
            EngineNo = Engine_Number;
        }
    }

    public class Body : Tag
    {
        public Body(byte[] TagID, string Body_Number)
            : base(TagID)
        {
            PunchBody = Body_Number;
        }
    }

    public class Engine : Tag
    {
        public Engine(byte[] TagID, string Engine_Number, string Chassis_Number)
            : base(TagID)
        {
            EngineNo = Engine_Number;
            ChassisNo = Chassis_Number;
        }
    }

    public class Reader
    {
        public Reader(string TCP_IP_Address, int TCP_Port, string Location_Name, byte Read_Type)
        {
        }

        public Reader()
        {
            // TODO: Complete member initialization
        }

        public Tag[] Poll()
        {
            return new Tag[] { };
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