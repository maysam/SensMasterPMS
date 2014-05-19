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
            string connectionString = ConfigurationManager.ConnectionStrings["SensMaster.Properties.Settings.pmsConnectionString"].ConnectionString;
            SQLConnection = new SqlConnection(connectionString);
            SQLConnection.Open();

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
            if (tags.Length == 1)
            {
                // single tag
                Tag tag = tags[0];
                using (SqlCommand cmd = new SqlCommand("sp_sm_singleTag", SQLConnection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("@strReaderID", SqlDbType.VarChar).Value = tag.reader.ID;
                    cmd.Parameters.Add("@strReaderIP", SqlDbType.VarChar).Value = tag.reader.TCP_IP_Address;
                    cmd.Parameters.Add("@strTagType", SqlDbType.VarChar).Value = Reader.SINGLETAG;
                    cmd.Parameters.Add("@strTagData", SqlDbType.VarChar).Value = tag.data;
                    cmd.Parameters.Add("@strTagID", SqlDbType.VarChar).Value = tag.ID;

                    cmd.ExecuteNonQuery();
                }

            }
            else if (tags.Length == 3)
            {
                if (tags.Any(tag => tag is Body) && tags.Any(tag => tag is Chassis) && tags.Any(tag => tag is Engine))
                {
                    Body body = (Body)tags.First(tag => tag is Body);
                    Chassis chassis = (Chassis)tags.First(tag => tag is Chassis);
                    Engine engine = (Engine)tags.First(tag => tag is Engine);
                    using (SqlCommand cmd = new SqlCommand("sp_sm_singleTag", SQLConnection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("@strReaderID", SqlDbType.VarChar).Value = body.reader.ID;
                        cmd.Parameters.Add("@strReaderIP", SqlDbType.VarChar).Value = body.reader.TCP_IP_Address;
                        cmd.Parameters.Add("@strChassisNo", SqlDbType.VarChar).Value = chassis.ChassisNo;
                        cmd.Parameters.Add("@strEngineNo", SqlDbType.VarChar).Value = engine.EngineNo;
                        cmd.Parameters.Add("@strBodyNo", SqlDbType.VarChar).Value = body.PunchBody;

                        cmd.ExecuteNonQuery();
                    }
                }
            }
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
                string ip = row["ReaderIP"] as string;
                Reader reader = new Reader(row["readerID"].ToString(), ip, Int16.Parse( row["tcpPort"].ToString()), "Damansara", byte.Parse( row["readTypeCode"].ToString()));
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
