using System.Collections.Concurrent;

namespace GameFrameX.SuperSocket.Kcp.Kcp;

/// <summary>
/// KCP 数据段对象池。
/// 避免频繁 new/GC，提升高频收发场景性能。
/// 使用 ConcurrentBag 保证线程安全。
/// </summary>
internal class KcpSegmentManager
{
    private readonly ConcurrentBag<KcpSegment> _pool;
    private readonly int _maxPoolSize;
    private int _currentCount;

    /// <summary>
    /// 初始化段对象池。
    /// </summary>
    /// <param name="maxPoolSize">池最大容量，默认 1024</param>
    public KcpSegmentManager(int maxPoolSize = 1024)
    {
        _maxPoolSize = maxPoolSize;
        _pool = new ConcurrentBag<KcpSegment>();
    }

    /// <summary>
    /// 从池中租用一个数据段。池为空时新建。
    /// </summary>
    /// <param name="dataSize">所需数据缓冲区大小</param>
    /// <returns>可用的 KCP 数据段</returns>
    public KcpSegment Rent(int dataSize)
    {
        if (_pool.TryTake(out var segment))
        {
            Interlocked.Decrement(ref _currentCount);
            segment.Reset(dataSize);
            return segment;
        }

        return new KcpSegment(dataSize);
    }

    /// <summary>
    /// 将数据段归还到池中。池满时直接丢弃（由 GC 回收）。
    /// </summary>
    /// <param name="segment">要归还的数据段</param>
    public void Return(KcpSegment segment)
    {
        if (segment == null)
        {
            return;
        }

        if (Interlocked.Increment(ref _currentCount) <= _maxPoolSize)
        {
            segment.Reset(0);
            _pool.Add(segment);
        }
        else
        {
            Interlocked.Decrement(ref _currentCount);
            // 池满，直接丢弃
        }
    }
}