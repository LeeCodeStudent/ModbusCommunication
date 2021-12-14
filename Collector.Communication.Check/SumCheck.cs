using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collector.Communication.Check
{
    public static class SumCheck
    {
        /// <summary>
        /// 累加和校验
        /// </summary>
        /// <param name="bs"></param>
        /// <returns></returns>
        private static byte Check(byte[] data)
        {
            int num = 0;
            //所有字节累加
            for (int i = 0; i < data.Length; i++)
            {
                num = (num + data[i]) % 0xFFFF;
            }
            byte ret = (byte)(num & 0xff);//只要最后一个字节
            return ret;
        }
    }
}
