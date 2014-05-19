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
}
