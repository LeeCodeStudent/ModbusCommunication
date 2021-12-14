using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collector.Communication.Common
{
    public enum DataFormat
    {
        //十进制202 二进制 1100 1010
        //ABCD：1100 1010
        //CDAB：1010 1100
        //BADC：0011 0101
        //DCBA：0101 0011

        /// <summary>
        /// 按照顺序排序
        /// </summary>
        ABCD = 0,
        /// <summary>
        /// 按照单字反转
        /// </summary>
        CDAB = 1,
        /// <summary>
        /// 按照双字反转
        /// </summary>
        BADC = 2,
        /// <summary>
        /// 按照倒序排序
        /// </summary>
        DCBA = 3
    }
    public class BitOperator
    {
        #region BitOperator方法

        /// <summary>
        /// 将一个字节转换成 8个 bool 值并返回 bool 数组
        /// </summary>
        /// <param name="b"></param>
        /// <param name="reverse">true，表示转换格式为ABCD 顺序；false，表示转换格式为 DCBA 倒序</param>
        /// <returns></returns>
        public bool[] GetBitArrayFromByte(byte source, bool reverse = false)
        {

            bool[] array = new bool[8];

            if (reverse)
            {
                //对于byte的每bit进行判定
                for (int i = 7; i >= 0; i--)
                {
                    //判定byte的最后一位是否为1，若为1，则是true；否则是false
                    array[i] = (source & 1) == 1;
                    //将byte右移一位
                    source = (byte)(source >> 1);
                }
            }
            else
            {
                //对于byte的每bit进行判定
                for (int i = 0; i <= 7; i++)
                {
                    //判定byte的最后一位是否为1，若为1，则是true；否则是false
                    array[i] = (source & 1) == 1;
                    //将byte右移一位
                    source = (byte)(source >> 1);
                }
            }
            return array;
        }

        /// <summary>
        /// 将一个字节数组按照一个字节 8 个bool值 转换成bool数组
        /// </summary>
        /// <param name="source"></param>
        /// <param name="reverse">true，表示转换格式为ABCD 顺序；false，表示转换格式为 DCBA 倒序</param>
        /// <returns></returns>
        public bool[] GetBitArrayFromByteArray(byte[] source, bool reverse = false)
        {
            // reverse = false {202,15} => {0101 0011 1111 0000} DCBA
            // reverse = true  {202,15} => {1100 1010 0000 1111} ABCD

            List<bool> res = new List<bool>();

            foreach (var item in source)
            {
                res.AddRange(GetBitArrayFromByte(item, reverse));
            }
            return res.ToArray();
        }

        /// <summary>
        /// 将 8 个bool值转换为一个字节
        /// </summary>
        /// <param name="source"></param>
        /// <param name="reverse">reverse = false 将 source 数组倒序；reverse = true 将source数组顺序 或 保持当前不变</param>
        /// <returns></returns>
        public byte GetByteFrom8Bits(bool[] source, bool reverse = false)
        {
            byte res = 0;
            if (!reverse)
            {
                Array.Reverse(source);
            }
            for (int i = 0; i < 8; i++)
            {
                res <<= 1;
                if (source[i])
                {
                    res += Convert.ToByte(source[i]);
                }
            }
            return res;
        }


        /// <summary>
        /// 将一个列表中的 bool 值按照每 8 个 bool 一个字节转换为字节数组
        /// </summary>
        /// <param name="source"></param>
        /// <param name="reverse">true，表示转换格式为ABCD 顺序；false，表示转换格式为 DCBA 倒序</param>
        /// <returns></returns>
        public byte[] GetByteFromBits(bool[] source, bool reverse = false)
        {
            int Length = source.Length;
            //字节的个数
            int byteLength = Length % 8 == 0 ? Length / 8 : Length / 8 + 1;

            List<bool> sourcrList = source.ToList<bool>();
            //数组不满8的整数倍，后几位填充false
            if (Length % 8 != 0)
            {
                for (int i = 0; i < byteLength * 8 - Length; i++)
                {
                    sourcrList.Add(false);
                }
            }
            byte[] resByte = new byte[byteLength];
            for (int i = 0; i < byteLength; i++)
            {
                bool[] tempArry = new bool[8];
                Array.Copy(sourcrList.ToArray(), i * 8, tempArry, 0, 8);
                resByte[i] = GetByteFrom8Bits(tempArry, reverse);
            }

            return resByte;
        }

        /// <summary>
        /// 将源字节数组根据起始位置和数量截取
        /// </summary>
        /// <param name="source"></param>
        /// <param name="start"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public byte[] GetByteArray(byte[] source, int start, int count)
        {
            if (source == null || source?.Length <= 0) return null;

            if (start < 0 || count < 0) return null;

            if (source.Length < start + count) return null;

            byte[] res = new byte[count];

            Array.Copy(source, start, res, 0, count);

            return res;
        }
        /// <summary>
        /// 将字节数组转换成ushort16位整型数组
        /// 用于 0x03 和 0x04 是将接收到的字节报文直接解析为ushort数组
        /// </summary>
        /// <param name="source"></param>
        /// <param name="dataFormat"></param>
        /// <returns></returns>
        public ushort[] GetUshortArrayFromByteArray(byte[] source,DataFormat dataFormat = DataFormat.ABCD)
        {
            ushort[] result = new ushort[source.Length / 2];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = GetUshortFrom2ByteArray(source, i * 2, dataFormat);
            }
            return result;
        }
        /// <summary>
        /// 将两个字节转换为ushort，默认是ABCD顺序
        /// </summary>
        /// <param name="source"></param>
        /// <param name="start"></param>
        /// <param name="dataFormat"></param>
        /// <returns></returns>
        public ushort GetUshortFrom2ByteArray(byte[] source, int start, DataFormat dataFormat = DataFormat.ABCD)
        {
            byte[] temp = Get2ByteArray(source, start, dataFormat);
            //BitConvert默认是 小端 显示，也就是说它的计算是按照CDAB计算的
            //需要将字节进行转换例如 12 => 00 0C，BitConverter 在计算的时候是按照倒序 0C 00 去做的
            Array.Reverse(temp);
            return BitConverter.ToUInt16(temp, 0);
        }

        /// <summary>
        /// 从字节数组中获取指定位置的2个字节
        /// </summary>
        /// <param name="source"></param>
        /// <param name="start"></param>
        /// <param name="dataFormat"></param>
        /// <returns></returns>
        public byte[] Get2ByteArray(byte[] source, int start, DataFormat dataFormat = DataFormat.ABCD)
        {
            byte[] Res = new byte[2];

            byte[] ResTemp = GetByteArray(source,start,2);

            if (ResTemp == null) return null;

            switch (dataFormat)
            {
                case DataFormat.ABCD:
                    Res = ResTemp;
                    break;
                case DataFormat.CDAB:
                    Res[0] = ResTemp[1];
                    Res[1] = ResTemp[0];
                    break;
                case DataFormat.BADC:
                    break;
                case DataFormat.DCBA:
                    break;
            }
            return Res;
        }
        /// <summary>
        /// 将一个16位的整数转换为二进制 并将每一位输出
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public char[] GetBoolArrayFromUshort(ushort source)
        {
            int sum;
            string result = "";
            for (int i = source; i >= 1; i = i / 2)
            {
                if (i % 2 == 0)
                {
                    sum = 0;
                }
                else
                {
                    sum = 1;
                }
                result = sum + result;
            }

            return result.ToArray();
        }
        #endregion
    }
}
