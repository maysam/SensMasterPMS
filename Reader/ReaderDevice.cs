using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensMaster
{
    interface ReaderDevice
    {
        void connect();
        bool ping();
        void Get_List_Of_Tag(Func<Tag[], bool> func);
    }
}
