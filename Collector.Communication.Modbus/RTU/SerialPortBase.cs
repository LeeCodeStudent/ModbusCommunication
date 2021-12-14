using Collector.Communication.Check;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collector.Communication.Modbus.RTU
{
    public class SerialPortBase: ModbusBase
    {
        #region Filed

        //创建串口对象
        private SerialPort _serialPort;


        #endregion

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
        public void Connect(string iPortName, int iBaudRate, Parity iParity, int iDataBits, StopBits iStopBits)
        {
            _serialPort = new SerialPort(iPortName, iBaudRate, iParity, iDataBits, iStopBits);
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }
            _serialPort.Open();
        }

        public void DisConnect()
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }
        }
        #endregion

        #region 发送和接收

        /// <summary>
        /// 验证CRC
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        protected bool CheckCRC(byte[] response)
        {
            byte[] CRC = CrcCheck.CalculateCRC16BigEndian(response, 0, (UInt32)response.Length - 2);

            if (CRC[0] == response[response.Length - 2] && CRC[1] == response[response.Length - 1])
            {
                return true;
            }
            else
            {
                return false;
            }
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
                        {
                            ms.Dispose();
                        }
                        //如果内存中已经有值了
                        else if (ms.Length > 0)
                        {
                            break;
                        }
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
