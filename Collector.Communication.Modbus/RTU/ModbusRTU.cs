﻿using System;
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

        #region Filed

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="iPortName">串口名称</param>
        /// <param name="iBaudRate">波特率</param>
        /// <param name="iDataBits">数据位</param>
        /// <param name="iStopBits">停止位</param>
        /// <param name="iParity">校验位</param>
        public ModbusRTU(string iPortName, int iBaudRate, int iDataBits, StopBits iStopBits, Parity iParity, string LogPath, string LogName) : base(iPortName, iBaudRate, iDataBits, iStopBits, iParity, LogPath, LogName) { }

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
        public async Task<Result<byte[]>> Read(byte iDevAdd, byte iFuncCode, ushort iStartAdd, ushort iLength)
        {
            var result = new Result<byte[]>();
            try
            {               
                //事件订阅
                result.SendRequest += Result_SendRequest;
                result.ReceiveResponse += Result_ReceiveResponse;
                //获取命令（组装报文）
                byte[] SendCommand = GetReadCommand(iDevAdd, iFuncCode, iStartAdd, iLength);

                result.Requst = string.Join(" ", SendCommand.Select(t => t.ToString("X2")));
                //发送并且接收报文
                byte[] Response = await SendAndReceive(SendCommand.ToArray());
                result.Response = string.Join(" ", Response.Select(t => t.ToString("X2")));

                //检验报文
                ushort byteLength = GetByteLength(iFuncCode, iLength);
                if (!CheckReadResponse(byteLength, SendCommand, Response))
                {
                    result.IsSucceed = false;
                    result.Err = "响应结果校验失败";
                    result.ErrList.Add("响应结果校验失败");
                }
                else
                {
                    result.Value = _bitOperator.GetByteArray(Response, 3, Response.Length - 5);
                }
            }
            catch (Exception ex)
            {
                result.IsSucceed = false;
                result.Err = ex.Message;
                result.ErrList.Add(ex.Message);
            }
            return result;
        }

        private void Result_SendRequest(Result e)
        {
            _txtFile.WriteLine(DateTime.Now.ToString() + " Send：" + Thread.CurrentThread.ManagedThreadId + "\n" + e.Requst);
            Console.WriteLine("Send：" + e.Requst);
        }

        private void Result_ReceiveResponse(Result e)
        {
            _txtFile.WriteLine(DateTime.Now.ToString() + " Receive：" + Thread.CurrentThread.ManagedThreadId + "\n" + e.Response);
            Console.WriteLine("Receive：" + e.Response);
        }

        /// <summary>
        /// 读取输出线圈 01
        /// </summary>
        /// <param name="iDevAdd"></param>
        /// <param name="iStartAdd"></param>
        /// <param name="iLength"></param>
        /// <returns></returns>
        public async Task<Result<bool[]>> ReadCoils(byte iDevAdd, ushort iStartAdd, ushort iLength)
        {
            try
            {
                var readResut = await Read(iDevAdd, fctReadCoils, iStartAdd, iLength);
                var result = new Result<bool[]>()
                {
                    IsSucceed = readResut.IsSucceed,
                    Err = readResut.Err,
                    ErrList = readResut.ErrList,
                    Requst = readResut.Requst,
                    Response = readResut.Response,
                };
                if (result.IsSucceed)
                    result.Value = _bitOperator.GetBitArrayFromByteArray(readResut.Value);
                return result;
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
        public async Task <Result<bool[]>> ReadDiscreteInputs(byte iDevAdd, ushort iStartAdd, ushort iLength)
        {
            try
            {
                var readResut = await Read(iDevAdd, fctReadCoils, iStartAdd, iLength);
                var result = new Result<bool[]>()
                {
                    IsSucceed = readResut.IsSucceed,
                    Err = readResut.Err,
                    ErrList = readResut.ErrList,
                    Requst = readResut.Requst,
                    Response = readResut.Response,
                };
                if (result.IsSucceed)
                    result.Value = _bitOperator.GetBitArrayFromByteArray(readResut.Value);
                return result;
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
        public async Task<Result<ushort[]>> ReadHoldingRegisters(byte iDevAdd, ushort iStartAdd, ushort iLength)
        {
            try
            {
                var readResut = await Read(iDevAdd, fctReadHoldingRegisters, iStartAdd, iLength);
                var result = new Result<ushort[]>()
                {
                    IsSucceed = readResut.IsSucceed,
                    Err = readResut.Err,
                    ErrList = readResut.ErrList,
                    Requst = readResut.Requst,
                    Response = readResut.Response,
                };
                if (result.IsSucceed)
                    result.Value = _bitOperator.GetUshortArrayFromByteArray(readResut.Value);
                return result;
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
        public async Task<Result<ushort[]>> ReadInputRegisters(byte iDevAdd, ushort iStartAdd, ushort iLength)
        {
            try
            {
                var readResut = await Read(iDevAdd, fctReadHoldingRegisters, iStartAdd, iLength);
                var result = new Result<ushort[]>()
                {
                    IsSucceed = readResut.IsSucceed,
                    Err = readResut.Err,
                    ErrList = readResut.ErrList,
                    Requst = readResut.Requst,
                    Response = readResut.Response,
                };
                if (result.IsSucceed)
                    result.Value = _bitOperator.GetUshortArrayFromByteArray(readResut.Value);
                return result;
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
        public async Task<Result<bool>> WriteSingle(byte iDevAdd, byte iFuncCode, ushort iStartAdd,byte[] SendCommand)
        {
            var result = new Result<bool>();
            try
            {
                //事件订阅
                result.SendRequest += Result_SendRequest;
                result.ReceiveResponse += Result_ReceiveResponse;
                //获取命令（组装报文）
                result.Requst = string.Join(" ", SendCommand.Select(t => t.ToString("X2")));
                //发送并且接收报文
                byte[] Response = await SendAndReceive(SendCommand.ToArray());
                result.Response = string.Join(" ", Response.Select(t => t.ToString("X2")));
                //验证报文正确性，写入单个数据的响应报文与写入报文一致，只需要验证CRC即可
                if (!CheckWriteSingleResponse(SendCommand, Response))
                {
                    result.IsSucceed = false;
                    result.Err = "响应结果校验失败";
                    result.ErrList.Add("响应结果校验失败");
                }
                else
                {
                    result.Value = true;
                }
            }
            catch (Exception ex)
            {
                result.IsSucceed = false;
                result.Err = ex.Message;
                result.ErrList.Add(ex.Message);
            }
            return result;
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
                var result = await WriteSingle(iDevAdd, iFuncCode, iStartAdd, SendCommand);
                return result.Value;
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
                var result = await WriteSingle(iDevAdd, iFuncCode, iStartAdd, SendCommand);
                return result.Value;
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
        public async Task<Result<bool>> WriteMultiple(byte iDevAdd, byte iFuncCode, ushort iStartAdd, byte[] SendCommand)
        {
            var result = new Result<bool>();
            try
            {
                //事件订阅
                result.SendRequest += Result_SendRequest;
                result.ReceiveResponse += Result_ReceiveResponse;
                //获取命令（组装报文）
                result.Requst = string.Join(" ", SendCommand.Select(t => t.ToString("X2")));
                //发送并且接收报文
                byte[] Response = await SendAndReceive(SendCommand.ToArray());
                result.Response = string.Join(" ", Response.Select(t => t.ToString("X2")));
                //验证报文正确性，写入单个数据的响应报文与写入报文一致，只需要验证CRC即可
                if (!CheckWriteMultipleResponse(SendCommand, Response))
                {
                    result.IsSucceed = false;
                    result.Err = "响应结果校验失败";
                    result.ErrList.Add("响应结果校验失败");
                }
                else
                {
                    result.Value = true;
                }
            }
            catch (Exception ex)
            {
                result.IsSucceed = false;
                result.Err = ex.Message;
                result.ErrList.Add(ex.Message);
            }
            return result;
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
                var result = await WriteMultiple(iDevAdd, iFuncCode, iStartAdd, SendCommand);
                return result.Value;
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
                var result = await WriteMultiple(iDevAdd, iFuncCode, iStartAdd, SendCommand);
                return result.Value;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion
    }
}
