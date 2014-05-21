using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Data.SqlClient;
using System.Configuration;
using System.Net;
using System.Timers;

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
            StartForDebugging();
        }


        internal void StartForDebugging()
        {

            eventLogger.WriteEntry("Monitoring started");
            string connectionString = ConfigurationManager.ConnectionStrings["SensMaster.Properties.Settings.pmsConnectionString"].ConnectionString;
            SQLConnection = new SqlConnection(connectionString);
            SQLConnection.Open();
            
            Int32 interval;
            if (Int32.TryParse(ConfigurationManager.AppSettings["db_polling_interval"], out interval))
            {
                DBtimer = new Timer(interval);
                DBtimer.Elapsed += DBtimer_Elapsed;
                DBtimer.Start();
            }
            if (Int32.TryParse(ConfigurationManager.AppSettings["reader_polling_interval"], out interval))
            {
                pmsTimer = new Timer(interval);
                pmsTimer.Elapsed += pmsTimer_Elapsed;
            }
            while (true) ;
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
        private ConnectionStatus last_status = ConnectionStatus.IDLE;
        private Timer pmsTimer;
        private Timer DBtimer;
        private void update_health_status(IPAddress ip, ConnectionStatus status)
        {
            lock(this){
            if (status != last_status)
            {
                using (SqlCommand cmd = new SqlCommand("sp_sm_reader_updateConnectionStatusCode", SQLConnection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@strIPAddress", SqlDbType.VarChar).Value = ip.ToString();
                    cmd.Parameters.Add("@intConnectionStatusCode", SqlDbType.Int).Value = status;
                    SqlParameter strResult = new SqlParameter("@strResult", SqlDbType.VarChar,10);
                    strResult.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(strResult);
                    SqlParameter strResultMessage = new SqlParameter("@strResultMessage", SqlDbType.VarChar, 50);
                    strResultMessage.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(strResultMessage);
                    cmd.ExecuteNonQuery();
                }
                last_status = status;
            }
        }
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
                    cmd.Parameters.Add("@strReaderIP", SqlDbType.VarChar).Value = tag.reader.IP.ToString();
                    cmd.Parameters.Add("@strTagType", SqlDbType.VarChar).Value = tag.type;
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
                        cmd.Parameters.Add("@strReaderIP", SqlDbType.VarChar).Value = body.reader.IP.ToString();
                        cmd.Parameters.Add("@strChassisNo", SqlDbType.VarChar).Value = chassis.ChassisNo;
                        cmd.Parameters.Add("@strEngineNo", SqlDbType.VarChar).Value = engine.EngineNo;
                        cmd.Parameters.Add("@strBodyNo", SqlDbType.VarChar).Value = body.PunchBody;

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            return true;
        }

        private void pmsTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            foreach (Reader reader in readers.Values)
            {
                reader.Poll(process);
            }
        }


        private void DBtimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            DBtimer.Stop();
            DataTable infoTable;
            SqlDataAdapter da;
            try
            {
                if (pmsTimer is Timer)
                    pmsTimer.Stop();
                infoTable = new DataTable();
                da = new SqlDataAdapter("SELECT * from Reader", SQLConnection);
                da.Fill(infoTable);

                foreach (DataRow row in infoTable.Rows)
                    try
                    {
                        string ip = row["ReaderIP"].ToString();
                        string location = "missing!";
                        Reader reader = null;
                        ReaderType reader_type = (ReaderType)byte.Parse(row["readTypeCode"].ToString());
                        int healthCheckPollingInterval = Int32.Parse(row["healthCheckPollingInterval"].ToString());
                        int healthCheckUpdateInterval = Int32.Parse(row["healthCheckUpdateInterval"].ToString());
                        if (readers.ContainsKey(ip))
                        {
                            reader = readers[ip];
                            reader.updateReader(location, reader_type, Int32.Parse(row["healthCheckPollingInterval"].ToString()), Int32.Parse(row["healthCheckUpdateInterval"].ToString()));
                        }
                        else
                        {
                            reader = new Reader(row["readerID"].ToString(), ip, Int16.Parse(row["tcpPort"].ToString()), location, reader_type, healthCheckPollingInterval, healthCheckUpdateInterval, update_health_status, exception_handler);
                            readers.Add(ip, reader);
                        }
                        reader.Start();
                    }
                    catch (Exception ex)
                    {
                        exception_handler(ex);
                    }
                if (pmsTimer != null)
                    pmsTimer.Start();
            }
            finally
            {
                DBtimer.Start();
            }
        }

        private void exception_handler(Exception ex)
        {
            if (ex != null)
            {
                eventLogger.WriteEntry(ex.Message, EventLogEntryType.Error);
            }
        }
    }
}
