using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace SensMaster
{
    public partial class FixedReaderService : ServiceBase
    {
        public FixedReaderService()
        {
            InitializeComponent();

            if (!System.Diagnostics.EventLog.SourceExists("FixedReader"))
            {
                System.Diagnostics.EventLog.CreateEventSource(
                    "FixedReader", "FixedReaderLog");
            }
            eventLogger.Source = "FixedReader";
            eventLogger.Log = "FixedReaderLog";
        }

        protected override void OnStart(string[] args)
        {
            eventLogger.WriteEntry("Monitoring started");
            pmsTimer.Start();
        }

        protected override void OnPause()
        {
            base.OnPause();
            pmsTimer.Stop();
        }

        protected override void OnContinue()
        {
            base.OnContinue();
            pmsTimer.Start();
        }
        protected override void OnStop()
        {
            pmsTimer.Stop();
        }

        private void pmsTimer_Tick(object sender, EventArgs e)
        {

        }
    }
}
