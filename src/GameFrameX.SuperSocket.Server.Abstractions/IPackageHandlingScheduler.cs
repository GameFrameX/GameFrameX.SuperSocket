using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using GameFrameX.SuperSocket.Primitives;
using GameFrameX.SuperSocket.Server.Abstractions.Session;

namespace GameFrameX.SuperSocket.Server.Abstractions
{
    /// <summary>
    /// 包处理调度器接口
    /// </summary>
    /// <typeparam name="TPackageInfo">包信息的类型参数</typeparam>
    public interface IPackageHandlingScheduler<TPackageInfo>
    {
        /// <summary>
        /// 初始化包处理调度器
        /// </summary>
        /// <param name="packageHandler">包处理器实例</param>
        /// <param name="errorHandler">错误处理委托，用于处理包处理过程中发生的异常</param>
        void Initialize(IPackageHandler<TPackageInfo> packageHandler, Func<IAppSession, PackageHandlingException<TPackageInfo>, ValueTask<bool>> errorHandler);

        /// <summary>
        /// 处理接收到的数据包
        /// </summary>
        /// <param name="session">当前会话实例</param>
        /// <param name="package">要处理的数据包</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>表示异步操作的任务</returns>
        ValueTask HandlePackage(IAppSession session, TPackageInfo package, CancellationToken cancellationToken);
    }
}