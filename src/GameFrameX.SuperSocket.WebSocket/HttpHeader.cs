using System;
using System.Buffers;
using System.Collections.Specialized;

namespace GameFrameX.SuperSocket.WebSocket
{
    /// <summary>
    /// HTTP头部信息类
    /// </summary>
    public class HttpHeader
    {
        /// <summary>
        /// 获取HTTP请求方法
        /// </summary>
        public string Method { get; private set; }

        /// <summary>
        /// 获取请求路径
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// 获取HTTP版本
        /// </summary>
        public string HttpVersion { get; private set; }

        /// <summary>
        /// 获取状态码
        /// </summary>
        public string StatusCode { get; private set; }

        /// <summary>
        /// 获取状态描述
        /// </summary>
        public string StatusDescription { get; private set; }

        /// <summary>
        /// 获取HTTP头部项集合
        /// </summary>
        public NameValueCollection Items { get; private set; }

        /// <summary>
        /// 私有构造函数
        /// </summary>
        private HttpHeader()
        {
        }

        /// <summary>
        /// 创建HTTP请求头部信息
        /// </summary>
        /// <param name="method">HTTP请求方法</param>
        /// <param name="path">请求路径</param>
        /// <param name="httpVersion">HTTP版本</param>
        /// <param name="items">头部项集合</param>
        /// <returns>返回HTTP头部信息实例</returns>
        public static HttpHeader CreateForRequest(string method, string path, string httpVersion, NameValueCollection items)
        {
            return new HttpHeader
            {
                Method = method,
                Path = path,
                HttpVersion = httpVersion,
                Items = items
            };
        }

        /// <summary>
        /// 创建HTTP响应头部信息
        /// </summary>
        /// <param name="httpVersion">HTTP版本</param>
        /// <param name="statusCode">状态码</param>
        /// <param name="statusDescription">状态描述</param>
        /// <param name="items">头部项集合</param>
        /// <returns>返回HTTP头部信息实例</returns>
        public static HttpHeader CreateForResponse(string httpVersion, string statusCode, string statusDescription, NameValueCollection items)
        {
            return new HttpHeader
            {
                HttpVersion = httpVersion,
                StatusCode = statusCode,
                StatusDescription = statusDescription,
                Items = items
            };
        }
    }
}