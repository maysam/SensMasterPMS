using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace SensMaster
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            if (!Environment.UserInteractive)
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] { new FixedReaderService() };
                ServiceBase.Run(ServicesToRun);
            }
            else
            {
                FixedReaderService sm = new FixedReaderService();
                Console.Write("Service debug run");
                sm.InternalStart();
                while (true) ;
            }
        }
    }
}
