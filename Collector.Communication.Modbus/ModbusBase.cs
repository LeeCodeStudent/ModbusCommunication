using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Collector.Communication.Common;
using Collector.Common.File;

namespace Collector.Communication.Modbus
{
    public class ModbusBase
    {        
        //创建Common字段
        protected BitOperator _bitOperator;

        protected TxtFile _txtFile;

        //接收超时时间
        protected int RecTimeOut { get; set; } = 2000;

        public ModbusBase(string LogPath, string LogName)
        {
            _txtFile = new TxtFile();

            _txtFile.Create(LogPath, LogName);

            _bitOperator = new BitOperator();
        }
        // 功能码 iFuncCode
        protected const byte fctReadCoils = 0x01;

        protected const byte fctReadDiscreteInputs = 0x2;

        protected const byte fctReadHoldingRegisters = 0x3;

        protected const byte fctReadInputRegisters = 0x4;

        protected const byte fctWriteSingleCoil = 0x5;

        protected const byte fctWriteSingleRegister = 0x6;

        protected const byte fctWriteMultipleCoils = 0x0F;

        protected const byte fctWriteMultipleRegisters = 0x10;

        protected const byte fctReadWriteMultipleRegister = 0x17;


        /// <summary>
        /// 根据请求的数据个数计算出数据字节的长度
        /// 读线圈时一个字节表示8个bool，读寄存器时两个字节表示一个short
        /// 需要用线圈或寄存器的个数计算数据字节数
        /// </summary>
        /// <param name="iFuncCode"></param>
        /// <param name="iLength"></param>
        /// <returns></returns>
        protected ushort GetByteLength(byte iFuncCode, ushort iLength)
        {
            switch (iFuncCode)
            {
                case 0x01:
                    return (ushort)(iLength % 8 == 0 ? iLength / 8 : iLength / 8 + 1);
                case 0x02:
                    return (ushort)(iLength % 8 == 0 ? iLength / 8 : iLength / 8 + 1);
                case 0x03:
                    return (ushort)(iLength * 2);
                case 0x04:
                    return (ushort)(iLength * 2);
                case 0x0F:
                    return (ushort)(iLength % 8 == 0 ? iLength / 8 : iLength / 8 + 1);
                case 0x10:
                    return (ushort)(iLength * 2);
                default:
                    return 0;
            }
        }
    }
}
