using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;

namespace SensMaster
{
    // Connection Status Code
    public enum ConnectionStatus
    {
        IDLE = 0x00,
        OK = 0x01,
        PING_FAIL = 0x02,
        NOT_RESPONDING = 0x03
    }
    // Read Type Code
    public enum ReaderType : byte
    {
        SINGLETAG = 0x01,
        MARRIAGETAG = 0x03
    }
    public class Reader
    {
        // Current OP Code
        public static readonly byte STOP_SEARCH = 0x00;
        public static readonly byte START_SEARCH = 0x01;
        public static readonly byte DFU = 0x02;

        private ReaderDevice device;
        public IPAddress IP;
        public int TCP_Port;
        private string Location_Name;
        public ReaderType Read_Type;
        public string ID;

        private ConnectionStatus status = ConnectionStatus.IDLE;
        public Timer health_check_poll_timer;
        public Timer health_check_update_timer;
        public Action<IPAddress, ConnectionStatus> do_update_health;
        private Action<Reader, Exception> logger;
        TcpClient tcpClient;

        public Reader(string id, IPAddress client_ip, Int32 client_port, string TCP_IP_Address, int port, string location, ReaderType Read_Type,
            int healthCheckPollingInterval, int healthCheckUpdateInterval, Action<IPAddress, ConnectionStatus> update_health, Action<Reader, Exception> logger)
        {
            IP = IPAddress.Parse(TCP_IP_Address);
            health_check_poll_timer = new Timer(healthCheckPollingInterval);
            health_check_poll_timer.Elapsed += health_check_poll_timer_Elapsed;
            health_check_update_timer = new Timer(healthCheckUpdateInterval);
            health_check_update_timer.Elapsed += health_check_update_timer_Elapsed;
            ID = id;
            this.TCP_Port = port;
            this.Location_Name = location;
            this.Read_Type = Read_Type;
            this.logger = logger;
            do_update_health = update_health;

            IPEndPoint ipLocalEndPoint = new IPEndPoint(client_ip, client_port);
                tcpClient = new TcpClient(ipLocalEndPoint);
            
            device = new RFIdentReader(this, tcpClient);
            device.connect();
        }

        public void updateReader(string location, ReaderType Read_Type, int healthCheckPollingInterval, int healthCheckUpdateInterval)
        {
            health_check_poll_timer.Interval = healthCheckPollingInterval;
            health_check_update_timer.Interval = healthCheckUpdateInterval;
            this.Location_Name = location;
            this.Read_Type = Read_Type;
        }

        private void health_check_update_timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            do_update_health(IP, status);
        }

        private void health_check_poll_timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (device.ping())
                {
                    status = ConnectionStatus.OK;
                }
                else
                {
                    status = ConnectionStatus.PING_FAIL;
                    do_update_health(IP, status);
                }
            }
            catch (Exception ex)
            {

                connection_failing(ex);
            }
        }
        DateTime waiting = DateTime.MinValue;
        public void post_process()
        {
            waiting = DateTime.MinValue;
        }
        public void Poll(Action<Tag[], Action> process)
        {
            //if (waiting == DateTime.MinValue)// || (DateTime.Now-waiting).TotalSeconds > 3000
            {
                waiting = DateTime.Now;
                device.Get_List_Of_Tag(process, post_process);
            }
        }

        public void Start()
        {
            health_check_poll_timer.Start();
            health_check_update_timer.Start();
        }

        public void Stop()
        {
            health_check_poll_timer.Stop();
            health_check_update_timer.Stop();
        }

        internal void connection_failing(Exception ex = null)
        {
            if (logger != null)
            {
                logger(this, ex);
            }
            waiting = DateTime.MinValue;
            status = ConnectionStatus.NOT_RESPONDING;
            //do_update_health(IP, status);
        }

        internal void connect(TcpClient tcpClient)
        {
            tcpClient.Connect(IP, TCP_Port);
        }

        public Tag ParseUserMemory(byte[] data_array)
        {
            return device.ParseUserMemory(data_array);
        }
    }
}