using System;
using System.Threading;
using System.Threading.Tasks;
using GameFrameX.SuperSocket.Server.Abstractions.Session;

namespace GameFrameX.SuperSocket.Server.Abstractions
{
    /// <summary>
    /// 包处理器接口
    /// </summary>
    /// <typeparam name="TReceivePackageInfo">接收包信息的类型</typeparam>
    public interface IPackageHandler<TReceivePackageInfo>
    {
        /// <summary>
        /// 处理接收到的数据包
        /// </summary>
        /// <param name="session">应用会话实例</param>
        /// <param name="package">接收到的数据包</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>表示异步操作的ValueTask</returns>
        ValueTask Handle(IAppSession session, TReceivePackageInfo package, CancellationToken cancellationToken);
    }
}