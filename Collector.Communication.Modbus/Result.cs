using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collector.Communication.Modbus
{

    public delegate void RequstEventHandler(Result e);

    public delegate void ResponseEventHandler(Result e);
    public class Result
    {
        public event RequstEventHandler SendRequest;

        public event ResponseEventHandler ReceiveResponse;

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSucceed { get; set; } = true;

        /// <summary>
        /// 异常消息
        /// </summary>
        public string Err { get; set; }

        /// <summary>
        /// 异常Code
        /// 408 连接失败
        /// </summary>
        public int ErrCode { get; set; }

        /// <summary>
        /// 详细异常
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// 异常集合
        /// </summary>
        public List<string> ErrList { get; set; } = new List<string>();

        /// <summary>
        /// 请求报文
        /// </summary>
        private string requst;
        public string Requst
        {
            get { return requst; }
            set
            {
                requst = value;
                SendRequest?.Invoke(this);
            }
        }

        /// <summary>
        /// 响应报文
        /// </summary>
        private string response;
        public string Response
        {
            get { return response; }
            set
            {
                response = value;
                ReceiveResponse?.Invoke(this);
            }
        }

        /// <summary>
        /// 耗时（毫秒）
        /// </summary>
        public double? TimeConsuming { get; private set; }

        /// <summary>
        /// 结束时间统计
        /// </summary>
        public Result EndTime()
        {
            TimeConsuming = (DateTime.Now - InitialTime).TotalMilliseconds;
            return this;
        }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime InitialTime { get; protected set; } = DateTime.Now;


    }

    /// <summary>
    /// 请求结果
    /// </summary>
    public class Result<T> : Result
    {
        public Result()
        {

        }

        public Result(T data)
        {
            Value = data;
        }

        public Result(Result result)
        {
            Assignment(result);
        }

        public Result(Result result, T data)
        {
            Assignment(result);
            Value = data;
        }

        /// <summary>
        /// 数据结果
        /// </summary>
        public T Value { get; set; }

        /// <summary>
        /// 结束时间统计
        /// </summary>
        internal new Result<T> EndTime()
        {
            base.EndTime();
            return this;
        }

        /// <summary>
        /// 赋值
        /// </summary>
        private void Assignment(Result result)
        {
            IsSucceed = result.IsSucceed;
            InitialTime = result.InitialTime;
            Err = result.Err;
            ErrList = result.ErrList;
            Requst = result.Requst;
            Response = result.Response;
            Exception = result.Exception;
            ErrCode = result.ErrCode;
        }
    }
}
