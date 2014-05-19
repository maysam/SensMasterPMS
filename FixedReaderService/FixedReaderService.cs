using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Data.SqlClient;
using System.Configuration;
using System.Threading.Tasks;

namespace SensMaster
{
   
    public partial class FixedReaderService : ServiceBase
    {
        SqlConnection SQLConnection;
        Dictionary<string, Reader> readers = new Dictionary<string, Reader>();


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
            string connectionString = ConfigurationManager.ConnectionStrings["SensMaster.Properties.Settings.pmsConnectionString"].ConnectionString ;
            SQLConnection = new SqlConnection(connectionString);
            SQLConnection.Open();
            Int32 interval;
            if(Int32.TryParse( ConfigurationManager.AppSettings["db_polling_interval"], out interval)){
                DBtimer.Interval  = interval;
                DBtimer.Start();
            }
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
            SQLConnection.Close();
        }
        private bool process(Tag[] tags)
        {
            return true;
        }

        private void pmsTimer_Tick(object sender, EventArgs e)
        {
            foreach (Reader reader in readers.Values)
            {
                reader.Poll(process);
            }
        }

        private void DBtimer_Tick(object sender, EventArgs e)
        {
            DataTable infoTable = new DataTable();
            SqlDataAdapter da = new SqlDataAdapter("SELECT * from Reader", SQLConnection);
            da.Fill(infoTable);

            foreach (DataRow row in infoTable.Rows)
            {
                string ip = row["currentReaderIP"] as string;
                Reader reader = new Reader(row["currentReaderIP"].ToString(), 22, "Damansara", Reader.SINGLETAG);
                if (readers.ContainsKey(ip))
                {
                    readers[ip] = reader;
                }
                else
                {
                    readers.Add(ip, reader);
                }
            }
        }
    }
}
