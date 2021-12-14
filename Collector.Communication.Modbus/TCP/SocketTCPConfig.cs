using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collector.Communication.Modbus.TCP
{
    public delegate void ConnectEventHandler(SocketTCPConfig e);

    public class SocketTCPConfig
    {
        public event ConnectEventHandler ConnectSuccess;

        /// <summary>
        /// 是否连接成功
        /// </summary>
        private bool isConnectSucceed = false;

        public bool IsConnectSucceed
        {
            get { return isConnectSucceed; }
            set
            {
                isConnectSucceed = value;
                ConnectSuccess?.Invoke(this);
            }
        }

        /// <summary>
        /// 已连接的服务器IP地址
        /// </summary>
        public string IP { get; set; }
        /// <summary>
        /// 已连接的服务器端口号
        /// </summary>
        public int Port { get; set; }
    }
}
