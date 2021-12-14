using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Collector.Communication.Check;
using System.Threading;
using Collector.Communication.Common;

namespace Collector.Communication.Modbus.RTU
{
    public class ModbusRTU : SerialPortBase
    {
        public ModbusRTU()
        {
            _bitOperator = new BitOperator();
        }
        #region Filed


        #endregion

        #region 读取数据

        /// <summary>
        /// 读取数据
        /// </summary>
        /// <param name="iDevAdd">从站地址</param>
        /// <param name="iFuncCode">功能码</param>
        /// <param name="iStartAdd">起始地址</param>
        /// <param name="iLength">请求数据的个数，即线圈或寄存器的数量</param>
        /// <returns></returns>
        public async Task<byte[]> Read(byte iDevAdd, byte iFuncCode, ushort iStartAdd, ushort iLength)
        {
            byte[] SendCommand = GetReadCommand(iDevAdd, iFuncCode, iStartAdd, iLength);
            var sendHex = SendCommand.Select(a => a.ToString("X2"));
            Console.WriteLine(string.Join(" ", sendHex.ToArray()));           

            int byteLength = GetByteLength(iFuncCode, iLength);

            byte[] response = await SendAndReceive(SendCommand.ToArray());

            var revHex = response.Select(a => a.ToString("X2"));
            Console.WriteLine(string.Join(" ", revHex.ToArray()));
            //验证报文正确性
            if (response?.Length == 5 + byteLength)
            {
                if (response[0] == iDevAdd && response[1] == iFuncCode && response[2] == byteLength && CheckCRC(response))
                {
                    return _bitOperator.GetByteArray(response, 3, response.Length - 5);
                }
            }
            return null;
        }

        /// <summary>
        /// 读取输出线圈 01
        /// </summary>
        /// <param name="iDevAdd"></param>
        /// <param name="iStartAdd"></param>
        /// <param name="iLength"></param>
        /// <returns></returns>
        public async Task<bool[]> ReadCoils(byte iDevAdd, ushort iStartAdd, ushort iLength)
        {
            try
            {
                byte[] byteRes = await Read(iDevAdd, fctReadCoils, iStartAdd, iLength);
                bool[] boolRes = _bitOperator.GetBitArrayFromByteArray(byteRes);
                return boolRes;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 读取输入线圈 02
        /// </summary>
        /// <param name="iDevAdd"></param>
        /// <param name="iStartAdd"></param>
        /// <param name="iLength"></param>
        /// <returns></returns>
        public async Task <bool[]> ReadDiscreteInputs(byte iDevAdd, ushort iStartAdd, ushort iLength)
        {
            try
            {
                byte[] byteRes = await Read(iDevAdd, fctReadDiscreteInputs, iStartAdd, iLength);
                bool[] boolRes = _bitOperator.GetBitArrayFromByteArray(byteRes);
                return boolRes;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 读取保持寄存器 03
        /// </summary>
        /// <param name="iDevAdd"></param>
        /// <param name="iStartAdd"></param>
        /// <param name="iLength"></param>
        /// <returns></returns>
        public async Task<ushort[]> ReadHoldingRegisters(byte iDevAdd, ushort iStartAdd, ushort iLength)
        {
            try
            {
                byte[] byteRes = await Read(iDevAdd, fctReadHoldingRegisters, iStartAdd, iLength);
                ushort[] Int16Res = _bitOperator.GetUshortArrayFromByteArray(byteRes);
                return Int16Res;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 读取输入寄存器 04
        /// </summary>
        /// <param name="iDevAdd"></param>
        /// <param name="iStartAdd"></param>
        /// <param name="iLength"></param>
        /// <returns></returns>
        public async Task<ushort[]> ReadInputRegisters(byte iDevAdd, ushort iStartAdd, ushort iLength)
        {
            try
            {
                byte[] byteRes = await Read(iDevAdd, fctReadInputRegisters, iStartAdd, iLength);
                ushort[] Int16Res = _bitOperator.GetUshortArrayFromByteArray(byteRes);
                return Int16Res;

            }
            catch (Exception)
            {
                return null;
            }

        }

        #endregion

        #region 写入单个数据

        /// <summary>
        /// 写入单个数据
        /// </summary>
        /// <param name="iDevAdd">从站地址</param>
        /// <param name="iFuncCode">功能码</param>
        /// <param name="iStartAdd">起始地址</param>
        /// <param name="SendCommand">拼接报文</param>
        /// <returns></returns>
        public async Task<bool> WriteSingle(byte iDevAdd, byte iFuncCode, ushort iStartAdd,byte[] SendCommand)
        {
            var sendHex = SendCommand.Select(a => a.ToString("X2"));
            Console.WriteLine("Send：" + string.Join(" ", sendHex.ToArray()));

            byte[] response = await SendAndReceive(SendCommand.ToArray());
            if (response != null)
            {
                var revHex = response.Select(a => a.ToString("X2"));
                Console.WriteLine("Receive：" + string.Join(" ", revHex.ToArray()));
                //验证报文正确性，写入单个数据的响应报文与写入报文一致，只需要验证CRC即可
                if (response[response.Length - 2] == SendCommand[SendCommand.Length - 2] && response[response.Length - 1] == SendCommand[SendCommand.Length - 1])
                {
                    Console.WriteLine("写入成功");
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 写入单个线圈 05
        /// </summary>
        /// <param name="iDevAdd"></param>
        /// <param name="iStartAdd"></param>
        /// <param name="value"></param>
        /// <param name="iFuncCode"></param>
        /// <returns></returns>
        public async Task<bool> WriteSingleCoil(byte iDevAdd, ushort iStartAdd, bool value, byte iFuncCode = fctWriteSingleCoil)
        {
            try
            {
                byte[] SendCommand = GetWriteSingleCommand(iDevAdd, iFuncCode, iStartAdd, value);
                return await WriteSingle(iDevAdd, iFuncCode, iStartAdd, SendCommand);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 写入单个寄存器 06
        /// </summary>
        /// <param name="iDevAdd"></param>
        /// <param name="iStartAdd"></param>
        /// <param name="value"></param>
        /// <param name="iFuncCode"></param>
        /// <returns></returns>
        public async Task<bool> WriteSingleRegister(byte iDevAdd, ushort iStartAdd, ushort value, byte iFuncCode = fctWriteSingleRegister)
        {
            try
            {
                byte[] SendCommand = GetWriteSingleCommand(iDevAdd, iFuncCode, iStartAdd, value);
                return await WriteSingle(iDevAdd, iFuncCode, iStartAdd, SendCommand);
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region 写入多个数据

        /// <summary>
        /// 写入多个数据
        /// </summary>
        /// <param name="iDevAdd">从站地址</param>
        /// <param name="iFuncCode">功能码</param>
        /// <param name="iStartAdd">起始地址</param>
        /// <param name="SendCommand">拼接报文</param>
        /// <returns></returns>
        public async Task<bool> WriteMultiple(byte iDevAdd, byte iFuncCode, ushort iStartAdd, byte[] SendCommand)
        {
            var sendHex = SendCommand.Select(a => a.ToString("X2"));
            Console.WriteLine("Send：" + string.Join(" ", sendHex.ToArray()));

            byte[] response = await SendAndReceive(SendCommand.ToArray());

            if (response != null)
            {
                var revHex = response.Select(a => a.ToString("X2"));
                Console.WriteLine("Receive：" + string.Join(" ", revHex.ToArray()));
                //验证报文正确性 返回报文固定为8个字节
                if (response.Length == 8)
                {
                    if (response[0] == iDevAdd && response[1] == iFuncCode && CheckCRC(response))
                    {
                        Console.WriteLine("写入成功");
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 写入多个线圈 0F
        /// </summary>
        /// <param name="iDevAdd">从站地址</param>
        /// <param name="iStartAdd">起始地址</param>
        /// <param name="iLength">要写入的线圈数量</param>
        /// <param name="values">要写入的值</param>
        /// <param name="iFuncCode">功能码</param>
        /// <returns></returns>
        public async Task<bool> WriteMultipleCoils(byte iDevAdd, ushort iStartAdd, ushort iLength, bool[] values, byte iFuncCode = fctWriteMultipleCoils)
        {
            try
            {
                byte[] SendCommand = GetWriteCoilsCommand(iDevAdd, iFuncCode, iStartAdd, iLength, values);
                return await WriteMultiple(iDevAdd, iFuncCode, iStartAdd, SendCommand);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 写入多个寄存器 10
        /// </summary>
        /// <param name="iDevAdd"></param>
        /// <param name="iStartAdd"></param>
        /// <param name="iLength"></param>
        /// <param name="values"></param>
        /// <param name="iFuncCode">0X10</param>
        /// <returns></returns>
        public async Task<bool> WriteMultipleRegisters(byte iDevAdd, ushort iStartAdd, ushort iLength, ushort[] values ,byte iFuncCode = fctWriteMultipleRegisters)
        {
            try
            {
                byte[] SendCommand = GetWriteRegistersCommand(iDevAdd, iFuncCode, iStartAdd, iLength, values);
                return await WriteMultiple(iDevAdd, iFuncCode, iStartAdd, SendCommand);
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion
    }
}
