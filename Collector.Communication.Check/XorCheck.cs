using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collector.Communication.Check
{
    public static class XorCheck
    {
        public static byte Check(byte[] data)
        {
            byte CheckCode = 0;
            int len = data.Length;
            for (int i = 0; i < len; i++)
            {
                CheckCode ^= data[i];
            }
            return CheckCode;
        }
    }
}
