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
        public string ID;
        public Reader reader;
        public string data = null;
        public Char type;

        public Tag(Reader reader, string data, string TagID)
        {
            ID = TagID;
            this.data = data;
            this.reader = reader;
        }
    }

    public class Chassis : Tag
    {
        public Chassis(Reader reader, string data, string TagID, string Chassis_Number, string Engine_Number)
            : base(reader, data, TagID)
        {
            type = 'C';
            ChassisNo = Chassis_Number;
            EngineNo = Engine_Number;
        }
    }

    public class Body : Tag
    {
        public Body(Reader reader, string data, string TagID, string Body_Number)
            : base(reader, data, TagID)
        {
            type = 'B';
            PunchBody = Body_Number;
        }
    }

    public class Engine : Tag
    {
        public Engine(Reader reader, string data, string TagID, string Engine_Number, string Chassis_Number)
            : base(reader, data, TagID)
        {
            type = 'E';
            EngineNo = Engine_Number;
            ChassisNo = Chassis_Number;
        }
    }
}
