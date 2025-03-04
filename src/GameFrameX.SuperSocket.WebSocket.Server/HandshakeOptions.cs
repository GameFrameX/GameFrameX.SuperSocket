namespace GameFrameX.SuperSocket.WebSocket.Server;

/// <summary>
/// WebSocket握手配置选项类
/// </summary>
public class HandshakeOptions
{
    /// <summary>
    /// 握手队列检查间隔时间，以秒为单位
    /// </summary>
    /// <value>默认值: 60秒</value>
    public int CheckingInterval { get; set; } = 60;

    /// <summary>
    /// 开放握手超时时间，以秒为单位
    /// </summary>
    /// <value>默认值: 120秒</value>
    public int OpenHandshakeTimeOut { get; set; } = 120;

    /// <summary>
    /// 关闭握手超时时间，以秒为单位
    /// </summary>
    /// <value>默认值: 120秒</value>
    public int CloseHandshakeTimeOut { get; set; } = 120;

    /// <summary>
    /// 握手验证器委托
    /// </summary>
    /// <remarks>
    /// 用于验证WebSocket握手请求的自定义验证函数
    /// </remarks>
    public Func<WebSocketSession, WebSocketPackage, ValueTask<bool>> HandshakeValidator { get; set; }
}