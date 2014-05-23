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
            InternalStart();
        }

        int server_port;
        IPAddress server_ip;

        internal void InternalStart()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["SensMaster.Properties.Settings.pmsConnectionString"].ConnectionString;
            SQLConnection = new SqlConnection(connectionString);
            SQLConnection.Open();
            eventLogger.WriteEntry("Monitoring started", EventLogEntryType.SuccessAudit);
          
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
            Int32.TryParse(ConfigurationManager.AppSettings["client_port"], out server_port);
            IPAddress.TryParse(ConfigurationManager.AppSettings["client_ip"], out server_ip);
            DBtimer_Elapsed(null, null);
        }
        protected override void OnPause()
        {
            base.OnPause();
            pmsTimer.Stop();
        }

        protected override void OnContinue()
        {
            base.OnContinue();
            DBtimer_Elapsed(null, null);
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
        private void process(Tag[] tags, Action post_process)
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
                    cmd.Parameters.Add("@strTagType", SqlDbType.VarChar).Value = tag.type.ToString();
                    cmd.Parameters.Add("@strTagData", SqlDbType.VarChar).Value = tag.data;
                    cmd.Parameters.Add("@strTagID", SqlDbType.VarChar).Value = tag.ID;
                    SqlParameter strResult = new SqlParameter("@strResult", SqlDbType.VarChar, 10);
                    strResult.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(strResult);
                    SqlParameter strResultMessage = new SqlParameter("@strResultMessage", SqlDbType.VarChar, 50);
                    strResultMessage.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(strResultMessage);

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
                    using (SqlCommand cmd = new SqlCommand("sp_sm_marriageTag", SQLConnection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("@strReaderID", SqlDbType.VarChar, 20).Value = body.reader.ID;
                        cmd.Parameters.Add("@strReaderIP", SqlDbType.VarChar, 23).Value = body.reader.IP.ToString();
                        cmd.Parameters.Add("@strChassisNo", SqlDbType.VarChar, 30).Value = chassis.ChassisNo;
                        cmd.Parameters.Add("@strEngineNo", SqlDbType.VarChar, 30).Value = engine.EngineNo;
                        cmd.Parameters.Add("@strBodyNo", SqlDbType.VarChar, 30).Value = body.PunchBody;
                        SqlParameter strResult = new SqlParameter("@strResult", SqlDbType.VarChar, 10);
                        strResult.Direction = ParameterDirection.Output;
                        cmd.Parameters.Add(strResult);
                        SqlParameter strResultMessage = new SqlParameter("@strResultMessage", SqlDbType.VarChar, 50);
                        strResultMessage.Direction = ParameterDirection.Output;
                        cmd.Parameters.Add(strResultMessage);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            post_process();
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
                        string reader_ip = row["ReaderIP"].ToString();
                        string location = "missing!";
                        Reader reader = null;
                        ReaderType reader_type = (ReaderType)byte.Parse(row["readTypeCode"].ToString());
                        int healthCheckPollingInterval = Int32.Parse(row["healthCheckPollingInterval"].ToString());
                        int healthCheckUpdateInterval = Int32.Parse(row["healthCheckUpdateInterval"].ToString());
                        if (readers.ContainsKey(reader_ip))
                        {
                            reader = readers[reader_ip];
                            reader.updateReader(location, reader_type, Int32.Parse(row["healthCheckPollingInterval"].ToString()), Int32.Parse(row["healthCheckUpdateInterval"].ToString()));
                        }
                        else
                        {
                            reader = new Reader(row["readerID"].ToString(), server_ip, server_port, reader_ip, Int16.Parse(row["tcpPort"].ToString()),
                                location, reader_type, 
                                healthCheckPollingInterval, healthCheckUpdateInterval, update_health_status, exception_handler);
                            readers.Add(reader_ip, reader);
                        }
                        reader.Start();
                    }
                    catch (Exception ex)
                    {
                        exception_handler(null,ex);
                    }
                if (pmsTimer != null)
                    pmsTimer.Start();
            }
            finally
            {
                DBtimer.Start();
            }
        }

        private void exception_handler(Reader reader, Exception ex)
        {
            if (ex != null)
            {
                short id = 0;
                if (reader != null)
                    short.TryParse(reader.ID, out id);
                eventLogger.WriteEntry(ex.Message, EventLogEntryType.Error, id);
            }
        }
    }
}
