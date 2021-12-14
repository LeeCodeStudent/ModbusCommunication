using Collector.Communication.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collector.Communication.Modbus.TCP
{
    public class ModbusTCP: SocketTCPBase
    {
        public ModbusTCP()
        {
            _bitOperator = new BitOperator();
        }
        #region Read

        /// <summary>
        /// 读取数据
        /// </summary>
        /// <param name="iDevAdd"></param>
        /// <param name="iFuncCode"></param>
        /// <param name="iStartAdd"></param>
        /// <param name="iLength"></param>
        /// <returns></returns>
        private async Task<byte[]> Read(byte iDevAdd, byte iFuncCode, ushort iStartAdd, ushort iLength)
        {
            byte[] res = new byte[1024];
            try
            {
                var checkHead = GetCheckHead(iFuncCode);
                //1 获取命令（组装报文）
                byte[] SendCommand = GetReadCommand(iDevAdd, iFuncCode, iStartAdd, iLength, checkHead);

                // 转换成16进制 只做查看 不做传输
                var sendHex = SendCommand.Select(a => a.ToString("X2"));
                Console.WriteLine("Send：" + string.Join(" ", sendHex.ToArray()));

                //读线圈时一个字节表示8个bool，读寄存器时两个字节表示一个short
                //需要用线圈或寄存器的个数计算数据字节数
                int byteLength = GetByteLength(iFuncCode, iLength);
                byte[] response = await SendAndReceive(SendCommand.ToArray());

                // 转换成16进制 只做查看 不做传输
                var revHex = response.Select(a => a.ToString("X2"));
                Console.WriteLine("Receive：" + string.Join(" ", revHex.ToArray()));
                //验证报文正确性
                if (response?.Length == 9 + byteLength)
                {
                    //验证前4个字节是否与发送报文的前4个字节一致
                    if (response[0] == SendCommand[0] && response[1] == SendCommand[1] && response[2] == SendCommand[2] && response[3] == SendCommand[3])
                    {
                        //验证报文长度是否正确
                        if (_bitOperator.GetUshortFrom2ByteArray(new byte[] { response[4], response[5] }, 0) == 3 + byteLength && response[8] == byteLength)
                        {
                            if (response[6] == iDevAdd && response[7] == iFuncCode)
                            {
                                Console.WriteLine("读取成功");
                                return _bitOperator.GetByteArray(response, 9, response.Length - 9);
                            }
                        }
                    }
                }


                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
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
        public async Task<bool[]> ReadDiscreteInputs(byte iDevAdd, ushort iStartAdd, ushort iLength)
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
        public async Task<ushort[]> ReadHoldingRegisters(byte iDevAdd, byte iStartAdd,  ushort iLength)
        {
            try
            {
                byte[] byteRes = await Read(iDevAdd, fctReadHoldingRegisters,iStartAdd, iLength);
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
        public async Task<ushort[]> ReadInputRegisters(byte iDevAdd, byte iStartAdd, ushort iLength)
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
        /// </summary>
        /// <param name="iDevAdd">从站地址</param>
        /// <param name="iFuncCode">功能码</param>
        /// <param name="iStartAdd">起始地址</param>
        /// <param name="SendCommand">拼接报文</param>
        /// <returns></returns>
        private async Task<bool> WriteSingle(byte iDevAdd, byte iFuncCode, ushort iStartAdd, byte[] SendCommand)
        {
            // 转换成16进制 只做查看 不做传输
            var sendHex = SendCommand.Select(a => a.ToString("X2"));
            Console.WriteLine("Send：" + string.Join(" ", sendHex.ToArray()));

            //读线圈时一个字节表示8个线圈数据
            //需要用数据个数计算字节数
            byte[] response = await SendAndReceive(SendCommand.ToArray());
            if (response != null)
            {
                // 转换成16进制 只做查看 不做传输
                var revHex = response.Select(a => a.ToString("X2"));
                Console.WriteLine("Receive：" + string.Join(" ", revHex.ToArray()));
                //验证报文正确性 返回报文固定为 12 个字节
                int res = 0;
                for (int i = 0; i < 12; i++)
                {
                    if (SendCommand[i] == response[i])
                    {
                        res++;
                    }
                }
                if (res == 12)
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
        /// <returns></returns>
        public async Task<bool> WriteSingleCoil(byte iDevAdd, ushort iStartAdd, bool value)
        {
            try
            {
                byte[] SendCommand = GetWriteSingleCommand(iDevAdd, fctWriteSingleCoil, iStartAdd,value);
                return await WriteSingle(iDevAdd, fctWriteSingleCoil, iStartAdd, SendCommand);
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
        /// <returns></returns>
        public async Task<bool> WriteSingleRegister(byte iDevAdd, ushort iStartAdd, short value)
        {
            try
            {
                byte[] SendCommand = GetWriteSingleCommand(iDevAdd, fctWriteSingleRegister, iStartAdd, value);
                return await WriteSingle(iDevAdd, fctWriteSingleCoil, iStartAdd, SendCommand);
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
        private async Task<bool> WriteMultiple(byte iDevAdd, byte iFuncCode, ushort iStartAdd, byte[] SendCommand)
        {
            // 转换成16进制 只做查看 不做传输
            var sendHex = SendCommand.Select(a => a.ToString("X2"));
            Console.WriteLine("Send：" + string.Join(" ", sendHex.ToArray()));

            //读线圈时一个字节表示8个线圈数据
            //需要用数据个数计算字节数
            byte[] response = await SendAndReceive(SendCommand.ToArray());

            if (response != null)
            {
                // 转换成16进制 只做查看 不做传输
                var revHex = response.Select(a => a.ToString("X2"));
                Console.WriteLine("Receive：" + string.Join(" ", revHex.ToArray()));
                //验证报文正确性 返回报文固定为 12 个字节 ,除了第 6 个字节为 06，其他与请求报文一致
                int res = 0;
                for (int i = 0; i < 12; i++)
                {
                    if (i == 5 && response[i] == 06)
                    {
                        res++;
                    }
                    else if (SendCommand[i] == response[i])
                    {
                        res++;
                    }
                }
                if (res == 12)
                {
                    Console.WriteLine("写入成功");
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 写入多个线圈 0F
        /// </summary>
        /// <param name="iDevAdd">从站地址</param>
        /// <param name="iStartAdd">起始地址</param>
        /// <param name="iLength">要写入的线圈数量，不一定等于values的长度</param>
        /// <param name="values">要写入的值</param>
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
        /// <param name="iLength">要写入的线圈数量，不一定等于values的长度</param>
        /// <returns></returns>
        public async Task<bool> WriteMultipleRegisters(byte iDevAdd, ushort iStartAdd, ushort iLength, ushort[] values, byte iFuncCode = fctWriteMultipleRegisters)
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
