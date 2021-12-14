using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Collector.Communication.Modbus.TCP
{
    public class SocketTCPBase: ModbusBase
    {

        #region Field

        protected IPEndPoint _ipEndPoint;

        protected Socket _socket;

        protected int ConnTimeOut = 2000;

        protected SocketTCPConfig _socketTcpConfig;

        #endregion

        public SocketTCPBase(IPEndPoint ipAndPoint, string LogPath, string LogName,int? ConnTimeOut = null) : base(LogPath, LogName)
        {
            if (ConnTimeOut.HasValue) this.ConnTimeOut = ConnTimeOut.Value;
            this._ipEndPoint = ipAndPoint;

            _socketTcpConfig = new SocketTCPConfig();
        }

        public SocketTCPBase(string ip, int port, string LogPath, string LogName, int? ConnTimeOut = null) : base(LogPath, LogName)
        {
            if (ConnTimeOut.HasValue) this.ConnTimeOut = ConnTimeOut.Value;
            this._ipEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);

            _socketTcpConfig = new SocketTCPConfig();
        }

        /// <summary>
        /// 连接
        /// </summary>
        /// <returns></returns>
        public Result Connect()
        {
            var result = new Result();
            _socket?.Close();
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                //连接
                _socket.Connect(_ipEndPoint);
                _socketTcpConfig.ConnectSuccess += _socketTCPConfig_ConnectSuccess;
                _socketTcpConfig.IP = this._ipEndPoint.Address.ToString();
                _socketTcpConfig.Port = this._ipEndPoint.Port;
                _socketTcpConfig.IsConnectSucceed = true;
                return result;
            }
            catch (Exception ex)
            {
                SafeClose(_socket);
                result.IsSucceed = false;
                result.Err = ex.Message;
                return result;
            }
        }

        private void _socketTCPConfig_ConnectSuccess(SocketTCPConfig e)
        {
            _txtFile.WriteLine(DateTime.Now.ToString() + "与服务器连接成功\n" + $"服务器IP地址：{e.IP}\n" + $"服务器端口号：{e.Port}\n" + $"当前线程：{Thread.CurrentThread.ManagedThreadId}\n");
            Console.WriteLine(DateTime.Now.ToString() + "与服务器连接成功\n" + $"服务器IP地址：{e.IP}\n" + $"服务器端口号：{e.Port}\n" + $"当前线程：{Thread.CurrentThread.ManagedThreadId}\n");
        }


        /// <summary>
        /// 关闭连接
        /// </summary>
        /// <returns></returns>
        public void DisConnect()
        {
            SafeClose(_socket);
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
                _socket.Send(send);

                //TCP接收
                MemoryStream ms = new MemoryStream();
                DateTime start = DateTime.Now;

                byte[] buffer = new byte[1024];
                while (true)
                {
                    await Task.Delay(10);

                    int count = _socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);
                    ms.Write(buffer, 0, count);

                    //接收超时
                    if ((DateTime.Now - start).TotalMilliseconds > this.RecTimeOut)
                        ms.Dispose();
                    //如果内存中已经有值了
                    else if (ms.Length > 0)
                        break;
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
        /// 检验读数据响应报文正确性
        /// </summary>
        /// <param name="iLength"></param>
        /// <param name="SendCommand"></param>
        /// <param name="Response"></param>
        /// <returns></returns>
        /// 读线圈
        /// Tx:132-00 04 00 00 00 06 01 01 00 00 00 0A
        /// Rx:133-00 04 00 00 00 05 01 01 02 55 00
        /// 读寄存器
        /// Tx:136-00 40 00 00 00 06 01 03 00 00 00 0A
        /// Rx:137-00 40 00 00 00 17 01 03 14 00 38 00 00 00 42 00 00 00 42 00 00 00 59 00 00 00 00 00 00
        protected bool CheckReadResponse(ushort byteLength, byte[] SendCommand, byte[] Response)
        {
            //验证报文长度是否正确
            if (Response?.Length == 9 + byteLength)
                //验证前4个字节是否与发送报文的前4个字节一致
                if (Response[0] == SendCommand[0] && Response[1] == SendCommand[1] && Response[2] == SendCommand[2] && Response[3] == SendCommand[3])
                    //验证字节长度是否正确
                    if (_bitOperator.GetUshortFrom2ByteArray(new byte[] { Response[4], Response[5] }, 0) == 3 + byteLength && Response[8] == byteLength)
                        //检验从站地址和功能码是否正确
                        if (Response[6] == SendCommand[6] && Response[7] == SendCommand[7])
                            return true;
            return false;
        }
        /// <summary>
        /// 检验写入单个数据响应报文正确性
        /// </summary>
        /// <param name="SendCommand"></param>
        /// <param name="Response"></param>
        /// <returns></returns>
        /// 写入单个线圈
        /// Tx:156-00 DA 00 00 00 06 01 05 00 00 FF 00
        /// Rx:157-00 DA 00 00 00 06 01 05 00 00 FF 00
        /// 写入单个寄存器
        /// Tx:172-01 0F 00 00 00 06 01 06 00 00 00 38
        /// Rx:173-01 0F 00 00 00 06 01 06 00 00 00 38
        protected bool CheckWriteSingleResponse(byte[] SendCommand, byte[] Response)
        {
            //请求报文与响应报文完全一致 固定为 12 个字节
            if (Response != null)
            {
                var q = from a in SendCommand
                        join b in Response on a equals b
                        select a;
                bool flag = SendCommand.Length == Response.Length && q.Count() == SendCommand.Length;
                return flag;
            }
            return false;
        }
        /// <summary>
        /// 检验写入多个数据响应报文正确性
        /// </summary>
        /// <param name="SendCommand"></param>
        /// <param name="Response"></param>
        /// <returns></returns>
        /// 写多个线圈
        /// Tx:238-01 AA 00 00 00 09 01 0F 00 00 00 0A 02 55 01
        /// Rx:239-01 AA 00 00 00 06 01 0F 00 00 00 0A
        /// 写多个寄存器
        /// Tx:216-01 74 00 00 00 1B 01 10 00 00 00 0A 14 00 43 00 38 00 00 00 00 00 4C 00 00 00 00 00 4D 00 00 00 59
        /// Rx:217-01 74 00 00 00 06 01 10 00 00 00 0A
        protected bool CheckWriteMultipleResponse(byte[] SendCommand, byte[] Response)
        {
            if (Response != null)
            {
                //验证报文正确性 返回报文固定为 12 个字节 ,除了第 6 个字节为 06，其他与请求报文一致
                int res = 0;
                for (int i = 0; i < 12; i++)
                {
                    if (i == 5)
                        if(Response[i] == 06) res++;
                    else 
                        if (SendCommand[i] == Response[i]) res++;     
                }
                if (res == 12) return true;
            }
            return false;
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
                SendCommand[10] = (BitConverter.GetBytes((ushort)value)[1]);    // 高位
                SendCommand[11] = (BitConverter.GetBytes((ushort)value)[0]);    // 低位
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
