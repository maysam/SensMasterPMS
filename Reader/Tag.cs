using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SensMaster
{
    public class Tag
    {
        public string PunchBody;
        public string ChassisNo;
        public string EngineNo;
        public byte[] ID;
        public Reader reader;
        public byte[] data = null;
        public Char type;

        public Tag(Reader reader, byte[] TagID)
        {
            ID = TagID;
            this.reader = reader;
        }
    }

    public class Chassis : Tag
    {
        public Chassis(Reader reader, byte[] TagID, string Chassis_Number, string Engine_Number)
            : base(reader, TagID)
        {
            type = 'C';
            ChassisNo = Chassis_Number;
            EngineNo = Engine_Number;
        }
    }

    public class Body : Tag
    {
        public Body(Reader reader, byte[] TagID, string Body_Number)
            : base(reader, TagID)
        {
            type = 'B';
            PunchBody = Body_Number;
        }
    }

    public class Engine : Tag
    {
        public Engine(Reader reader, byte[] TagID, string Engine_Number, string Chassis_Number)
            : base(reader, TagID)
        {
            type = 'E';
            EngineNo = Engine_Number;
            ChassisNo = Chassis_Number;
        }
    }
}
