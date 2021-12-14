using Collector.Communication.Check;
using Collector.Communication.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Collector.Communication.Modbus.RTU
{
    public class SerialPortBase: ModbusBase
    {
        #region Filed

        /// <summary>
        /// 串行端口对象
        /// </summary>
        protected SerialPort _serialPort;
        /// <summary>
        /// 
        /// </summary>
        protected SerialPortConfig _serialPortConfig;


        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="iPortName">串口名称</param>
        /// <param name="iBaudRate">波特率</param>
        /// <param name="iDataBits">数据位</param>
        /// <param name="iStopBits">停止位</param>
        /// <param name="iParity">校验位</param>
        public SerialPortBase(string iPortName, int iBaudRate, int iDataBits, StopBits iStopBits, Parity iParity, string LogPath, string LogName) : base( LogPath, LogName)
        {
            if (_serialPort == null) _serialPort = new SerialPort();
            _serialPort.PortName = iPortName;
            _serialPort.BaudRate = iBaudRate;
            _serialPort.DataBits = iDataBits;
            _serialPort.StopBits = iStopBits;
            _serialPort.Encoding = Encoding.ASCII;
            _serialPort.Parity = iParity;
            _bitOperator = new BitOperator();
            _serialPortConfig = new SerialPortConfig();
        }

        #region Method

        #region 打开和关闭串口

        /// <summary>
        /// 连接串口方法
        /// </summary>
        /// <param name="iPortName"></param>
        /// <param name="iBaudRate"></param>
        /// <param name="iParity"></param>
        /// <param name="iDataBits"></param>
        /// <param name="iStopBits"></param>
        public Result Connect()
        {
            var ports = SerialPort.GetPortNames();
            var result = new Result();
            _serialPort?.Close();
            try
            {
                //连接
                _serialPort.Open();
                _serialPortConfig.ConnectSuccess += _serialPortConfig_ConnectSuccess; ;
                _serialPortConfig.iPortName = _serialPort.PortName;
                _serialPortConfig.iBaudRate = _serialPort.BaudRate;
                _serialPortConfig.iDataBits = _serialPort.DataBits;
                _serialPortConfig.iStopBits = _serialPort.StopBits;
                _serialPortConfig.iParity = _serialPort.Parity;
                _serialPortConfig.IsConnectSucceed = true;
                return result;
            }
            catch (Exception ex)
            {
                result.IsSucceed = false;
                result.Err = ex.Message;
                return result;
            }
        }

        private void _serialPortConfig_ConnectSuccess(SerialPortConfig e)
        {
            _txtFile.WriteLine(DateTime.Now.ToString() + "串口连接成功：\n" + $"串口号：{e.iPortName}\n" + $"波特率：{e.iBaudRate}\n" + $"数据位：{e.iDataBits}\n" + $"停止位：{e.iStopBits}\n" + $"校验位：{e.iParity}\n"+ $"当前线程：{Thread.CurrentThread.ManagedThreadId}\n");
            Console.WriteLine(DateTime.Now.ToString() + "串口连接成功：\n" + $"串口号：{e.iPortName}\n" + $"波特率：{e.iBaudRate}\n" + $"数据位：{e.iDataBits}\n" + $"停止位：{e.iStopBits}\n" + $"校验位：{e.iParity}\n" + $"当前线程：{Thread.CurrentThread.ManagedThreadId}\n");
        }

        public Result DisConnect()
        {
            var result = new Result();
            try
            {
                _serialPort.Close();
            }
            catch (Exception ex)
            {
                result.IsSucceed = false;
                result.Err = ex.Message;
            }
            return result;
        }
        #endregion

        #region 发送和接收

        /// <summary>
        /// 验证响应报文CRC正确性
        /// </summary>
        /// <param name="Response"></param>
        /// <returns></returns>
        protected bool CheckCRC(byte[] Response)
        {
            byte[] CRC = CrcCheck.CalculateCRC16BigEndian(Response, 0, (UInt32)Response.Length - 2);
            if (CRC[0] == Response[Response.Length - 2] && CRC[1] == Response[Response.Length - 1])
                return true;
            return false;
        }
        protected async Task<byte[]> SendAndReceive(byte[] send)
        {
            try
            {
                //串口发送
                _serialPort.Write(send, 0, send.Length);

                //串口接收
                MemoryStream ms = new MemoryStream();
                DateTime start = DateTime.Now;

                byte[] buffer = new byte[1024];
                while (true)
                {
                    await Task.Delay(10);
                    if (_serialPort.BytesToRead > 0)
                    {
                        int count = _serialPort.Read(buffer, 0, buffer.Length);
                        ms.Write(buffer, 0, count);
                    }
                    else
                    {
                        //接收超时
                        if ((DateTime.Now - start).TotalMilliseconds > this.RecTimeOut)
                            ms.Dispose();
                        //如果内存中已经有值了
                        else if (ms.Length > 0)
                            break;
                    }
                }
                //接收到的报文数据
                return ms.ToArray();
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
        /// Tx:120-01 01 00 00 00 0A BC 0D
        /// Rx:121-01 01 02 55 00 86 AC
        /// 读寄存器
        /// Tx:000-01 03 00 00 00 0A C5 CD
        /// Rx:001-01 03 14 00 38 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 0E 02
        protected bool CheckReadResponse(ushort byteLength, byte[] SendCommand, byte[] Response)
        {            
            //验证报文长度是否正确
            if (Response?.Length == 5 + byteLength)
                //检验从站地址、功能码、字节长度、CRC
                if (Response[0] == SendCommand[0] && Response[1] == SendCommand[1] && Response[2] == byteLength && CheckCRC(Response))
                    return true;
            return false;
        }
        /// <summary>
        /// 检验写入单个数据响应报文正确性
        /// </summary>
        /// <param name="SendCommand"></param>
        /// <param name="Response"></param>
        /// <returns></returns>
        /// 写单个线圈
        /// Tx:016-01 05 00 00 FF 00 8C 3A
        /// Rx:017-01 05 00 00 FF 00 8C 3A
        /// 写单个寄存器
        /// Tx:114-01 06 00 00 00 2D 49 D7
        /// Rx:115-01 06 00 00 00 2D 49 D7
        protected bool CheckWriteSingleResponse(byte[] SendCommand, byte[] Response)
        {
            //请求报文与响应报文完全一致 固定为 8 个字节
            //RTU只检查响应报文CRC 与 请求报文CRC是否一致即可
            if (Response[Response.Length - 2] == SendCommand[SendCommand.Length - 2] && Response[Response.Length - 1] == SendCommand[SendCommand.Length - 1])
                return true;
            return false;
        }
        /// <summary>
        /// 检验写入多个数据响应报文正确性
        /// </summary>
        /// <param name="SendCommand"></param>
        /// <param name="Response"></param>
        /// <returns></returns>
        /// 写多个线圈
        /// Tx:042-01 0F 00 00 00 0A 02 1F 00 ED 08
        /// Rx:043-01 0F 00 00 00 0A D5 CC
        /// 写多个寄存器
        /// Tx:090-01 10 00 00 00 0A 14 00 0C 00 00 00 17 00 00 00 22 00 00 00 2D 00 00 00 38 00 00 E8 16
        /// Rx:091-01 10 00 00 00 0A 40 0E
        protected bool CheckWriteMultipleResponse(byte[] SendCommand, byte[] Response)
        {               
            //验证报文正确性 返回报文固定为8个字节
            if (Response != null)
                if (Response.Length == 8)
                    if (Response[0] == SendCommand[0] && Response[1] == SendCommand[1] && Response[2] == SendCommand[2] && Response[3] == SendCommand[3] && Response[4] == SendCommand[4] && Response[5] == SendCommand[5])
                        if(CheckCRC(Response))
                        return true;
            return false;
        }
        /// <summary>
        /// 获取读取命令
        /// </summary>
        /// <param name="iDevAdd">站号</param>
        /// <param name="iFuncCode">功能码</param>
        /// <param name="iStartAdd">寄存器起始地址</param>
        /// <param name="iLength">读取数据个数</param>
        /// <returns></returns>
        protected byte[] GetReadCommand(byte iDevAdd, byte iFuncCode, ushort iStartAdd, ushort iLength)
        {
            //拼接报文
            List<byte> SendCommand = new List<byte>();

            SendCommand.Add(iDevAdd);
            SendCommand.Add(iFuncCode);
            //BitConvert默认是 小端 显示，也就是说它的计算是按照CDAB计算的
            SendCommand.Add((BitConverter.GetBytes(iStartAdd)[1]));    //SendCommand.Add((byte)(iStartAdd / 256));   // 高位
            SendCommand.Add((BitConverter.GetBytes(iStartAdd)[0]));    //SendCommand.Add((byte)(iStartAdd % 256));   // 低位
            //BitConvert默认是 小端 显示，也就是说它的计算是按照CDAB计算的    
            SendCommand.Add((BitConverter.GetBytes(iLength)[1]));      //SendCommand.Add((byte)(iLength / 256));     // 高位
            SendCommand.Add((BitConverter.GetBytes(iLength)[0]));      //SendCommand.Add((byte)(iLength % 256));     // 低位

            //一、通过查表
            //byte[] CRC = CrcCheck.Crc16BigEndian(SendCommand.ToArray(),0, (UInt32)SendCommand.Count);
            //二、通过计算
            byte[] CRC = CrcCheck.CalculateCRC16BigEndian(SendCommand.ToArray(), 0, (UInt32)SendCommand.Count);
            SendCommand.AddRange(CRC);

            return SendCommand.ToArray();
        }

        /// <summary>
        /// 获取写入单个命令
        /// </summary>
        /// <param name="iDevAdd"></param>
        /// <param name="iFuncCode"></param>
        /// <param name="iStartAdd"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected byte[] GetWriteSingleCommand(byte iDevAdd, byte iFuncCode, ushort iStartAdd, object value)
        {
            //拼接报文
            List<byte> SendCommand = new List<byte>();
            SendCommand.Add(iDevAdd);
            SendCommand.Add(iFuncCode);                   
            SendCommand.Add((BitConverter.GetBytes(iStartAdd)[1]));    //SendCommand.Add((byte)(iStartAdd / 256));    // 高位
            SendCommand.Add((BitConverter.GetBytes(iStartAdd)[0]));    //SendCommand.Add((byte)(iStartAdd % 256));    // 低位
            if (iFuncCode == fctWriteSingleCoil)
            {
                SendCommand.Add((byte)((bool)value ? 0xFF : 0x00));//固定写法：FF 00表示闭合 00 00表示断开，其他数值非法
                SendCommand.Add(0x00);
            }
            if (iFuncCode == fctWriteSingleRegister)
            {
                SendCommand.Add(BitConverter.GetBytes((ushort)value)[1]);    // 高位
                SendCommand.Add(BitConverter.GetBytes((ushort)value)[0]);    // 低位  
            }
            #region CRC16校验
            //一、通过查表
            //byte[] CRC = CrcCheck.Crc16BigEndian(SendCommand.ToArray(),0, (UInt32)SendCommand.Count);
            //二、通过计算
            byte[] CRC = CrcCheck.CalculateCRC16BigEndian(SendCommand.ToArray(), 0, (UInt32)SendCommand.Count);
            #endregion
            SendCommand.AddRange(CRC);

            return SendCommand.ToArray();
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
            //拼接报文
            List<byte> SendCommand = new List<byte>();
            SendCommand.Add(iDevAdd);
            SendCommand.Add(iFuncCode);
            SendCommand.Add((BitConverter.GetBytes(iStartAdd)[1]));   //SendCommand.Add((byte)(iStartAdd / 256));    // 高位
            SendCommand.Add((BitConverter.GetBytes(iStartAdd)[0]));   //SendCommand.Add((byte)(iStartAdd % 256));    // 低位
            SendCommand.Add((BitConverter.GetBytes(iLength)[1]));     //SendCommand.Add((byte)(iLength / 256));      // 高位
            SendCommand.Add((BitConverter.GetBytes(iLength)[0]));     //SendCommand.Add((byte)(iLength % 256));      // 低位
            SendCommand.Add((BitConverter.GetBytes(byteLength)[0]));
            return SendCommand.ToArray();
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
            #region CRC16校验
            //一、通过查表
            //byte[] CRC = CrcCheck.Crc16BigEndian(SendCommand.ToArray(),0, (UInt32)SendCommand.Count);
            //二、通过计算
            byte[] CRC = CrcCheck.CalculateCRC16BigEndian(SendCommand.ToArray(), 0, (UInt32)SendCommand.Count);
            #endregion
            SendCommand.AddRange(CRC);
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
            #region CRC16校验
            //一、通过查表
            //byte[] CRC = CrcCheck.Crc16BigEndian(SendCommand.ToArray(),0, (UInt32)SendCommand.Count);
            //二、通过计算
            byte[] CRC = CrcCheck.CalculateCRC16BigEndian(SendCommand.ToArray(), 0, (UInt32)SendCommand.Count);
            #endregion
            SendCommand.AddRange(CRC);
            return SendCommand.ToArray();
        }

        #endregion 

        #endregion
    }
}
