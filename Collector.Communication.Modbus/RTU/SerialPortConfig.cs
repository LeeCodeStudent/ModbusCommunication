using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collector.Communication.Modbus.RTU
{
    public delegate void ConnectEventHandler(SerialPortConfig e);

    public class SerialPortConfig
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
        /// 串口名称
        /// </summary>
        public string iPortName { get; set; }
        /// <summary>
        /// 波特率
        /// </summary>
        public int iBaudRate { get; set; }
        /// <summary>
        /// 数据位
        /// </summary>
        public int iDataBits { get; set; }
        /// <summary>
        /// 停止位
        /// </summary>
        public StopBits iStopBits { get; set; }
        /// <summary>
        /// 校验位
        /// </summary>
        public Parity iParity { get; set; }

    }
}
