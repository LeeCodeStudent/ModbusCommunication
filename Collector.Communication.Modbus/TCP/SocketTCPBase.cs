using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Collector.Communication.Common;

namespace Collector.Communication.Modbus.TCP
{
    public class SocketTCPBase: ModbusBase
    {

        #region Field

        private IPEndPoint ipAndPoint;

        protected Socket socket;

        private int ConnTimeOut = 2000;

        #endregion

        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public bool Connect(string ip, int port, int? timeout = null)
        {
            if (timeout.HasValue) this.ConnTimeOut = timeout.Value;
            this.ipAndPoint = new IPEndPoint(IPAddress.Parse(ip), port);

            socket?.Close();
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                //连接
                socket.Connect(ipAndPoint);
                return true;
            }
            catch (Exception ex)
            {
                SafeClose(socket);
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        /// <returns></returns>
        public void DisConnect()
        {
            SafeClose(socket);
        }
        protected void SafeClose(Socket socket)
        {
            try
            {
                if (socket?.Connected ?? false) socket?.Shutdown(SocketShutdown.Both);//正常关闭连接
            }
            catch { }

            try
            {
                socket?.Close();
            }
            catch { }
        }

        /// <summary>
        /// 发送并且接收响应报文
        /// </summary>
        /// <param name="send"></param>
        /// <returns></returns>
        protected async Task<byte[]> SendAndReceive(byte[] send)
        {
            try
            {
                //TCP发送
                socket.Send(send);

                //TCP接收
                MemoryStream ms = new MemoryStream();
                DateTime start = DateTime.Now;

                byte[] buffer = new byte[1024];
                while (true)
                {
                    await Task.Delay(10);

                    int count = socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);
                    ms.Write(buffer, 0, count);

                    //接收超时
                    if ((DateTime.Now - start).TotalMilliseconds > this.RecTimeOut)
                    {
                        ms.Dispose();
                    }
                    //如果内存中已经有值了
                    else if (ms.Length > 0)
                    {
                        break;
                    }
                }
                //接收到的报文数据
                return  ms.ToArray();
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 获取随机校验头
        /// </summary>
        /// <param name="seed"></param>
        /// <returns></returns>
        protected byte[] GetCheckHead(int seed)
        {
            var random = new Random(DateTime.Now.Millisecond + seed);
            return new byte[] { (byte)random.Next(255), (byte)random.Next(255) };
        }

        /// <summary>
        /// 获取读取命令
        /// </summary>
        /// <param name="iDevAdd">站号</param>
        /// <param name="iFuncCode">功能码</param>
        /// <param name="iStartAdd">寄存器起始地址</param>
        /// <param name="iLength">读取数据个数</param>
        /// <param name="check"></param>
        /// <returns></returns>
        protected byte[] GetReadCommand(byte iDevAdd, byte iFuncCode, ushort iStartAdd, ushort iLength, byte[] check = null)
        {
            byte[] SendCommand = new byte[12];

            SendCommand[0] = (check?[0] ?? 0x19);
            SendCommand[1] = (check?[1] ?? 0xB2);//Client发出的检验信息
            SendCommand[2] = (0x00);
            SendCommand[3] = (0x00);//表示tcp/ip 的协议的modbus的协议
            SendCommand[4] = (0x00);
            SendCommand[5] = (0x06);//表示的是该字节以后的字节长度

            SendCommand[6] = (iDevAdd);
            SendCommand[7] = (iFuncCode);
            SendCommand[8] = (BitConverter.GetBytes(iStartAdd)[1]);
            SendCommand[9] = (BitConverter.GetBytes(iStartAdd)[0]);//寄存器地址
            SendCommand[10] = (BitConverter.GetBytes(iLength)[1]);
            SendCommand[11] = (BitConverter.GetBytes(iLength)[0]);//表示request 寄存器的长度(寄存器个数)
            return SendCommand.ToArray();
        }

        /// <summary>
        /// 获取写入单个命令
        /// </summary>
        /// <param name="iDevAdd"></param>
        /// <param name="iFuncCode"></param>
        /// <param name="iStartAdd"></param>
        /// <param name="value"></param>
        /// <param name="check"></param>
        /// <returns></returns>
        protected byte[] GetWriteSingleCommand(byte iDevAdd, byte iFuncCode, ushort iStartAdd, object value, byte[] check = null)
        {
            byte[] SendCommand = new byte[12];
            SendCommand[0] = check?[0] ?? 0x19;
            SendCommand[1] = check?[1] ?? 0xB2;//Client发出的检验信息 
            SendCommand[2] = (0x00);
            SendCommand[3] = (0x00);//表示tcp/ip 的协议的modbus的协议
            SendCommand[4] = (0x00);
            SendCommand[5] = (0x06);//表示的是该字节以后的字节长度

            SendCommand[6] = iDevAdd;//站号
            SendCommand[7] = iFuncCode; //功能码
            SendCommand[8] = BitConverter.GetBytes(iStartAdd)[1];
            SendCommand[9] = BitConverter.GetBytes(iStartAdd)[0];//寄存器地址

            if (iFuncCode == fctWriteSingleCoil)
            {
                SendCommand[10] = (byte)((bool)value ? 0xFF : 0x00);     //此处只可以是FF表示闭合00表示断开，其他数值非法
                SendCommand[11] = 0x00;
            }
            if (iFuncCode == fctWriteSingleRegister)
            {
                SendCommand[10] = (BitConverter.GetBytes((short)value)[1]);    // 高位
                SendCommand[11] = (BitConverter.GetBytes((short)value)[0]);    // 低位
            }
            return SendCommand;
        }

        /// <summary>
        /// 获取写入多个命令
        /// </summary>
        /// <param name="iDevAdd"></param>
        /// <param name="iFuncCode"></param>
        /// <param name="iStartAdd"></param>
        /// <param name="iLength"></param>
        /// <param name="byteLength"></param>
        /// <param name="check"></param>
        /// <returns></returns>
        protected byte[] GetWriteMultipleCommand(byte iDevAdd, byte iFuncCode, ushort iStartAdd, ushort iLength, ushort byteLength, byte[] check = null)
        {
            byte[] SendCommand = new byte[13];
            SendCommand[0] = check?[0] ?? 0x19;
            SendCommand[1] = check?[1] ?? 0xB2;//检验信息，用来验证response是否串数据了           
            SendCommand[2] = (0x00);
            SendCommand[3] = (0x00);//表示tcp/ip 的协议的modbus的协议
            SendCommand[4] = BitConverter.GetBytes(7 + byteLength)[1];
            SendCommand[5] = BitConverter.GetBytes(7 + byteLength)[0];

            SendCommand[6] = iDevAdd;//站号
            SendCommand[7] = iFuncCode; //功能码
            SendCommand[8] = BitConverter.GetBytes(iStartAdd)[1];
            SendCommand[9] = BitConverter.GetBytes(iStartAdd)[0];//寄存器地址
            SendCommand[10] = BitConverter.GetBytes(iLength)[1]; ;
            SendCommand[11] = BitConverter.GetBytes(iLength)[0]; ;
            SendCommand[12] = (byte)(byteLength);

            return SendCommand;
        }

        /// <summary>
        /// 获取写入多个线圈命令
        /// </summary>
        /// <param name="iDevAdd"></param>
        /// <param name="iFuncCode"></param>
        /// <param name="iStartAdd"></param>
        /// <param name="iLength"></param>
        /// <param name="values"></param>
        /// <param name="check"></param>
        /// <returns></returns>
        protected byte[] GetWriteCoilsCommand(byte iDevAdd, byte iFuncCode, ushort iStartAdd, ushort iLength, bool[] values, byte[] check = null)
        {
            int byteLength = GetByteLength(iFuncCode, iLength);

            List<byte> SendCommand = GetWriteMultipleCommand(iDevAdd, iFuncCode, iStartAdd, iLength, (ushort)byteLength).ToList();

            byte[] dataByte = _bitOperator.GetByteFromBits(values);
            for (int i = 0; i < byteLength; i++)
            {
                SendCommand.Add(dataByte[i]);
            }
            return SendCommand.ToArray();
        }

        /// <summary>
        /// 获取写入多个寄存器命令
        /// </summary>
        /// <param name="iDevAdd"></param>
        /// <param name="iFuncCode"></param>
        /// <param name="iStartAdd"></param>
        /// <param name="iLength"></param>
        /// <param name="values"></param>
        /// <param name="check"></param>
        /// <returns></returns>
        protected byte[] GetWriteRegistersCommand(byte iDevAdd, byte iFuncCode, ushort iStartAdd, ushort iLength, ushort[] values, byte[] check = null)
        {
            int byteLength = GetByteLength(iFuncCode, iLength);

            List<byte> SendCommand = GetWriteMultipleCommand(iDevAdd, iFuncCode, iStartAdd, iLength, (ushort)byteLength).ToList();

            for (int i = 0; i < iLength; i++)
            {
                SendCommand.Add((BitConverter.GetBytes(values[i]))[1]);
                SendCommand.Add((BitConverter.GetBytes(values[i]))[0]);
            }
            return SendCommand.ToArray();
        }
    }
}
