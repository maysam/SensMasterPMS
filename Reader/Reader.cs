using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
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
        private Action<Exception> logger;

        public Reader(string id, string TCP_IP_Address, int port, string location, ReaderType Read_Type,
            int healthCheckPollingInterval, int healthCheckUpdateInterval, Action<IPAddress, ConnectionStatus> update_health, Action<Exception> logger)
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
            device = new RFIdentReader(this);
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

        public void Poll(Func<Tag[], bool> Display)
        {
            device.Get_List_Of_Tag(Display);
        }

        public Tag ParseUserMemory(byte[] read_data)
        {
            byte[] id = new byte[8];
            string tempNumber = "", Number1 = null, Number2 = null;

            Buffer.BlockCopy(read_data, 0, id, 0, 8);
            for (int i = 0; i < 40; i++)
            {
                if (i < 8)
                {
                    id[i] = read_data[i];
                }
                else if (i == 9)
                {
                    if (read_data[i] != 0x7C)
                    {
                        throw new Exception("Invalid Tag(10)");
                    }
                }
                else if (i > 9)
                {
                    if (read_data[i] == 0x00)
                    {
                        break;
                    }
                    else
                        if (read_data[i] == 0x7C)
                        {
                            Number1 = tempNumber;
                            tempNumber = "";
                        }
                        else
                        {
                            tempNumber += (char)read_data[i];
                        }
                }
            }
            Number2 = tempNumber;
            switch ((char)read_data[8])
            {
                case 'E':
                    return new Engine(this, read_data, Number1, Number2);
                case 'C':
                    return new Chassis(this, read_data, Number1, Number2);
                case 'B':
                    return new Body(this, read_data, Number1);
                default:
                    throw new Exception("Invalid Tag");
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
                logger(ex);
            }
            status = ConnectionStatus.NOT_RESPONDING;
            do_update_health(IP, status);
        }
    }
}