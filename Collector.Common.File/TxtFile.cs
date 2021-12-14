using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collector.Common.File
{
    /// <summary>
    /// TXT文件写入类
    /// </summary>
    public class TxtFile
    {
        private FileStream fileStream;
        private StreamWriter streamWriter;

        public TxtFile()
        {

        }

        /// <summary>
        /// 创建文件目录并创建TXT文件
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="fileName">文件名</param>
        public void Create(string path, string fileName)
        {
            DirectoryInfo di = new DirectoryInfo(path);
            if (!di.Exists)
                di.Create();
            fileStream = new FileStream(path + fileName, FileMode.Create);
            streamWriter = new StreamWriter(fileStream);
        }

        /// <summary>
        /// 写入数据
        /// </summary>
        /// <param name="message"></param>
        public void WriteLine(string message)
        {
            streamWriter.WriteLine(message);    //开始写入
            streamWriter.Flush();           //清空缓冲区
        }

        /// <summary>
        /// 关闭文件写入
        /// </summary>
        public void Close()
        {
            streamWriter.Close();
            fileStream.Close();
            streamWriter.Dispose();
            fileStream.Dispose();
        }
    }
}
