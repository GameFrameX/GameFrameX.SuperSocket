using System;
using System.Threading;
using System.Threading.Tasks;

namespace GameFrameX.SuperSocket.Server.Abstractions.Session;

/// <summary>
/// 游戏应用会话接口
/// </summary>
public interface IGameAppSession
{
    /// <summary>
    /// 获取会话唯一标识符
    /// </summary>
    /// <value>会话ID字符串</value>
    string SessionID { get; }

    /// <summary>
    /// 获取会话是否处于连接状态
    /// </summary>
    /// <value>如果会话已连接则为true，否则为false</value>
    bool IsConnected { get; }

    /// <summary>
    /// 向客户端发送字节数组数据
    /// </summary>
    /// <param name="data">要发送的字节数组数据</param>
    /// <param name="cancellationToken">取消令牌，用于取消发送操作</param>
    /// <returns>表示异步发送操作的ValueTask</returns>
    ValueTask SendAsync(byte[] data, CancellationToken cancellationToken = default);

    /// <summary>
    /// 向客户端发送只读内存数据
    /// </summary>
    /// <param name="data">要发送的只读内存数据</param>
    /// <param name="cancellationToken">取消令牌，用于取消发送操作</param>
    /// <returns>表示异步发送操作的ValueTask</returns>
    ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);
}