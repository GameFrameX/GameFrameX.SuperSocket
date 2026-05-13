using System;
using System.Buffers.Binary;

namespace GameFrameX.SuperSocket.Kcp.Kcp
{
    /// <summary>
    /// KCP 协议核心实现，移植自 https://github.com/skywind3000/kcp/blob/master/ikcp.c
    /// 提供可靠的 ARQ 传输能力，运行在 UDP 之上。
    /// </summary>
    internal class KcpCore
    {
        // === 连接状态 ===
        private uint _conv;
        private uint _mtu;
        private uint _mss;
        private int _state;

        // === 发送相关 ===
        private uint _snd_una;
        private uint _snd_nxt;
        private uint _rcv_nxt;

        // === 窗口 ===
        private uint _ssthresh;
        private uint _rmt_wnd;
        private uint _cwnd;
        private uint _probe;

        // === 时间 ===
        private uint _current;
        private uint _interval;
        private uint _ts_flush;
        private uint _ts_probe;
        private uint _probe_wait;
        private uint _xmit;

        // === RTT 估计 ===
        private int _rx_rttval;
        private int _rx_srtt;
        private int _rx_rto;
        private int _rx_minrto;

        // === 拥塞控制 ===
        private int _fastresend;
        private int _fastlimit;
        private bool _nocwnd;
        private bool _nodelay;
        private int _stream;

        // === 日志回调 ===
        private Action<string> _log;

        // === 数据结构 ===
        private readonly KcpSegmentList _snd_buf = new KcpSegmentList();
        private readonly KcpSegmentList _rcv_buf = new KcpSegmentList();
        private readonly KcpSegmentList _snd_queue = new KcpSegmentList();
        private readonly KcpSegmentList _rcv_queue = new KcpSegmentList();
        private readonly KcpAckList _acklist = new KcpAckList();
        private readonly KcpSegmentManager _segmentManager;

        // === 缓冲区 ===
        private byte[] _buffer;

        /// <summary>
        /// KCP 需要发送 UDP 包时的回调。参数为待发送的原始字节。
        /// </summary>
        public Action<Memory<byte>> Output { get; set; }

        /// <summary>
        /// 获取当前会话 ID。
        /// </summary>
        public uint Conv => _conv;

        /// <summary>
        /// 获取当前等待发送的段数（发送缓冲 + 发送队列）。
        /// </summary>
        public int WaitSnd => _snd_buf.Count + _snd_queue.Count;

        /// <summary>
        /// 获取连接状态。0=可用, -1=已断开。
        /// </summary>
        public int State => _state;

        /// <summary>
        /// 初始化 KCP 实例。
        /// </summary>
        /// <param name="conv">会话 ID</param>
        /// <param name="segmentManager">段对象池（null 则内部创建默认池）</param>
        /// <param name="log">日志回调（可选）</param>
        public KcpCore(uint conv, KcpSegmentManager segmentManager = null, Action<string> log = null)
        {
            _conv = conv;
            _segmentManager = segmentManager ?? new KcpSegmentManager();
            _log = log;

            _snd_una = 0;
            _snd_nxt = 0;
            _rcv_nxt = 0;
            _ts_probe = 0;
            _probe_wait = 0;
            _probe = 0;

            _mtu = KcpConstants.IKCP_MTU_DEF;
            _mss = _mtu - KcpConstants.IKCP_OVERHEAD;

            _rmt_wnd = KcpConstants.IKCP_WND_RCV;
            _cwnd = 32;
            _ssthresh = (uint)KcpConstants.IKCP_WND_MAX;  // 初始 ssthresh 较大，允许慢启动阶段充分增长

            _rx_rttval = 0;
            _rx_srtt = 0;
            _rx_rto = KcpConstants.IKCP_RTO_DEF;
            _rx_minrto = KcpConstants.IKCP_RTO_MIN;

            _fastresend = 0;
            _fastlimit = KcpConstants.IKCP_FASTACK_LIMIT;
            _nocwnd = false;
            _nodelay = false;
            _stream = 0;

            _interval = KcpConstants.IKCP_INTERVAL;
            _ts_flush = KcpConstants.IKCP_INTERVAL;
            _xmit = 0;
            _state = KcpConstants.IKCP_STATE_AVAILABLE;

            int bufferSize = (int)(_mtu + KcpConstants.IKCP_OVERHEAD) * 3;
            _buffer = new byte[bufferSize];
        }

        #region === 配置方法 ===

        /// <summary>
        /// 设置 MTU（最大传输单元）。小于 50 将抛异常。
        /// </summary>
        /// <param name="mtu">MTU 值</param>
        public void SetMtu(uint mtu)
        {
            if (mtu < KcpConstants.IKCP_MTU_MIN)
                throw new ArgumentException($"MTU must be >= {KcpConstants.IKCP_MTU_MIN}", nameof(mtu));

            _mtu = mtu;
            _mss = _mtu - KcpConstants.IKCP_OVERHEAD;

            int newSize = (int)(_mtu + KcpConstants.IKCP_OVERHEAD) * 3;
            if (_buffer.Length < newSize)
            {
                _buffer = new byte[newSize];
            }
        }

        /// <summary>
        /// 设置发送和接收窗口大小。
        /// </summary>
        /// <param name="sendWindow">发送窗口大小（此实现中暂未使用，远端窗口由对端通告）</param>
        /// <param name="receiveWindow">本端接收窗口大小</param>
        public void SetWindowSize(int sendWindow, int receiveWindow)
        {
            if (sendWindow < 0)
                throw new ArgumentException("Send window must be >= 0", nameof(sendWindow));
            if (receiveWindow < 0)
                throw new ArgumentException("Receive window must be >= 0", nameof(receiveWindow));

            _rmt_wnd = (uint)Math.Min(receiveWindow, KcpConstants.IKCP_WND_MAX);
        }

        /// <summary>
        /// 设置 NoDelay 模式参数。
        /// </summary>
        /// <param name="nodelay">0=关闭, 1=开启快速重传, 2=极速模式</param>
        /// <param name="interval">Update 间隔（毫秒），最小 10</param>
        /// <param name="resend">快速重传阈值，0=关闭</param>
        /// <param name="nc">是否关闭拥塞控制（1=关闭）</param>
        public void SetNoDelay(int nodelay, int interval, int resend, int nc)
        {
            _nodelay = nodelay > 0;

            if (nodelay == 2)
                _stream = 1;
            else
                _stream = 0;

            if (interval > 0)
            {
                _interval = (uint)Math.Max(interval, 10);
                if (_interval > 5000)
                    _interval = 5000;
            }

            if (resend >= 0)
                _fastresend = resend;

            _nocwnd = nc != 0;
        }

        /// <summary>
        /// 设置为普通模式（延迟优先）。
        /// </summary>
        public void SetNormalMode()
        {
            SetNoDelay(0, 40, 0, 0);
        }

        /// <summary>
        /// 设置为快速模式（推荐游戏使用，最低延迟）。
        /// </summary>
        public void SetFastMode()
        {
            SetNoDelay(1, 10, 2, 1);
        }

        #endregion

        #region === 核心方法 ===

        /// <summary>
        /// 发送应用层数据。数据会被分片后进入发送队列。
        /// </summary>
        /// <param name="data">要发送的数据</param>
        /// <returns>发送的字节数，负数表示错误</returns>
        public int Send(ReadOnlySpan<byte> data)
        {
            if (_state != KcpConstants.IKCP_STATE_AVAILABLE)
                return KcpConstants.IKCP_STATE_DEAD;

            if (data.Length == 0)
                return 0;

            // 计算需要多少个分片
            int count;
            if (_mss > 0)
                count = (data.Length + (int)_mss - 1) / (int)_mss;
            else
                count = 1;

            if (count == 0)
                count = 1;

            if (count > 255)
                return -2;

            for (int i = 0; i < count; i++)
            {
                int size = Math.Min((int)_mss, data.Length - i * (int)_mss);
                var seg = _segmentManager.Rent(size);

                seg.Conv = _conv;
                seg.Cmd = KcpConstants.IKCP_CMD_PUSH;
                seg.Frg = (byte)(_stream == 0 ? (count - i - 1) : 0);
                seg.Wnd = 0;
                seg.Ts = 0;
                seg.Sn = 0;
                seg.Una = 0;
                seg.Len = (uint)size;
                seg.Resendts = 0;
                seg.Rto = 0;
                seg.Fastack = 0;
                seg.Xmit = 0;

                data.Slice(i * (int)_mss, size).CopyTo(seg.Data);

                _snd_queue.AddLast(seg);
            }

            return data.Length;
        }

        /// <summary>
        /// 接收 KCP 重组后的完整消息。
        /// </summary>
        /// <param name="buffer">接收缓冲区</param>
        /// <returns>接收的字节数，负数表示错误</returns>
        public int Recv(Span<byte> buffer)
        {
            if (_state != KcpConstants.IKCP_STATE_AVAILABLE)
                return KcpConstants.IKCP_STATE_DEAD;

            if (_rcv_queue.Count == 0)
                return -1;

            int peekSize = PeekSizeInternal();
            if (peekSize < 0)
                return -2;

            if (peekSize > buffer.Length)
                return -3;

            if (_stream != 0)
            {
                // 流模式
                int totalRead = 0;
                while (_rcv_queue.Count > 0 && totalRead < buffer.Length)
                {
                    var seg = _rcv_queue.First;
                    int copyLen = Math.Min((int)seg.Len, buffer.Length - totalRead);
                    new ReadOnlySpan<byte>(seg.Data, 0, copyLen).CopyTo(buffer.Slice(totalRead));
                    totalRead += copyLen;

                    if (copyLen < seg.Len)
                    {
                        int remaining = (int)seg.Len - copyLen;
                        Array.Copy(seg.Data, copyLen, seg.Data, 0, remaining);
                        seg.Len = (uint)remaining;
                        break;
                    }
                    else
                    {
                        _rcv_queue.RemoveFirst();
                        _segmentManager.Return(seg);
                    }
                }

                return totalRead;
            }
            else
            {
                // 消息模式
                bool recover = _rcv_queue.Count >= _rmt_wnd;

                int totalRead = 0;
                while (_rcv_queue.Count > 0)
                {
                    var seg = _rcv_queue.First;
                    if ((int)seg.Len > buffer.Length - totalRead)
                        break;

                    new ReadOnlySpan<byte>(seg.Data, 0, (int)seg.Len).CopyTo(buffer.Slice(totalRead));
                    totalRead += (int)seg.Len;

                    _rcv_queue.RemoveFirst();
                    _segmentManager.Return(seg);

                    if (seg.Frg == 0)
                        break;
                }

                MoveRcvBufToQueue();

                // 快速恢复
                if (recover && _rcv_queue.Count < _rmt_wnd)
                {
                    if (_cwnd < _rmt_wnd)
                        _cwnd = _rmt_wnd;
                }

                return totalRead;
            }
        }

        /// <summary>
        /// 处理收到的 UDP 原始数据。
        /// </summary>
        /// <param name="data">UDP 包数据</param>
        /// <returns>0 表示成功，负数表示错误</returns>
        public int Input(ReadOnlySpan<byte> data)
        {
            if (_state != KcpConstants.IKCP_STATE_AVAILABLE)
                return KcpConstants.IKCP_STATE_DEAD;

            uint oldUna = _snd_una;

            while (true)
            {
                if (data.Length < KcpConstants.IKCP_OVERHEAD)
                    break;

                uint conv = BinaryPrimitives.ReadUInt32LittleEndian(data);
                byte cmd = data[4];
                byte frg = data[5];
                ushort wnd = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(6));
                uint ts = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(8));
                uint sn = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(12));
                uint una = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(16));
                uint len = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(20));

                data = data.Slice(KcpConstants.IKCP_OVERHEAD);

                if (data.Length < len)
                    return -2;

                if (conv != _conv)
                    return -3;

                if (cmd != KcpConstants.IKCP_CMD_PUSH &&
                    cmd != KcpConstants.IKCP_CMD_ACK &&
                    cmd != KcpConstants.IKCP_CMD_WASK &&
                    cmd != KcpConstants.IKCP_CMD_WINS)
                    return -4;

                _rmt_wnd = wnd;

                // 处理 UNA：移除所有 sn < una 的发送段
                ParseUna(una);

                // 更新本地 snd_una 为 snd_buf 中最小的 sn（即所有已确认的段之后的位置）
                if (_snd_buf.Count > 0)
                    _snd_una = _snd_buf.First.Sn;
                else
                    _snd_una = _snd_nxt;

                // 根据命令类型分别处理
                if (cmd == KcpConstants.IKCP_CMD_ACK)
                {
                    // ACK: 更新 RTT + 移除已确认段
                    UpdateAck(sn, ts);
                    ParseAck(sn);
                    ParseFastack(sn, _rcv_nxt);
                }
                else if (cmd == KcpConstants.IKCP_CMD_PUSH)
                {
                    if (IsTimeLess(sn, _rcv_nxt + _rmt_wnd))
                    {
                        _acklist.Add(sn, ts);

                        if (!IsTimeLess(sn, _rcv_nxt))
                        {
                            var newseg = new KcpSegment((int)len);
                            newseg.Conv = conv;
                            newseg.Cmd = cmd;
                            newseg.Frg = frg;
                            newseg.Wnd = wnd;
                            newseg.Ts = ts;
                            newseg.Sn = sn;
                            newseg.Una = una;
                            newseg.Len = len;

                            ParseData(newseg, data.Slice(0, (int)len));
                        }
                    }
                }
                else if (cmd == KcpConstants.IKCP_CMD_WASK)
                {
                    // 远端请求窗口探测
                    _probe |= (uint)KcpConstants.IKCP_PROBE_ASK;
                }
                // IKCP_CMD_WINS: 窗口探测响应，无需处理

                data = data.Slice((int)len);
            }

            // 如果有新的段被确认，更新拥塞窗口
            if (_snd_una != oldUna)
            {
                if (_cwnd < _rmt_wnd)
                    _cwnd++;
            }

            return 0;
        }

        /// <summary>
        /// 驱动 KCP 状态机。检查重传、发送 ACK、窗口探测等。
        /// </summary>
        /// <param name="current">当前时间戳（毫秒）</param>
        public uint Update(uint current)
        {
            _current = current;

            if (_state != KcpConstants.IKCP_STATE_AVAILABLE)
                return _current;

            uint slap = _current - _ts_flush;

            if ((int)slap >= 10000 || (int)slap < -10000)
            {
                _ts_flush = _current;
                slap = 0;
            }

            if (slap >= _interval || IsTimeLess(_ts_flush, _current))
            {
                _ts_flush = _current + _interval;
                Flush();
            }

            return _ts_flush;
        }

        /// <summary>
        /// 检查下次 Update 的时间（不实际执行 Update）。
        /// </summary>
        /// <param name="current">当前时间戳（毫秒）</param>
        /// <returns>下次 Update 时间戳</returns>
        public uint Check(uint current)
        {
            if (_state != KcpConstants.IKCP_STATE_AVAILABLE)
                return current;

            uint ts_flush = _ts_flush;
            uint next = ts_flush;

            if (_snd_buf.Count > 0)
            {
                var seg = _snd_buf.First;
                if (seg != null && IsTimeLess(seg.Resendts, next) && seg.Resendts != 0)
                    next = seg.Resendts;
            }

            uint tm = _current + _interval;
            if (IsTimeLess(next, tm))
                next = tm;

            return next;
        }

        /// <summary>
        /// 检查是否有完整的消息可读。
        /// </summary>
        public bool PeekCanRecv()
        {
            if (_rcv_queue.Count == 0)
                return false;

            return PeekSizeInternal() >= 0;
        }

        /// <summary>
        /// 获取下一个完整消息的大小。
        /// </summary>
        /// <returns>消息大小，负数表示无消息</returns>
        public int PeekSize()
        {
            return PeekSizeInternal();
        }

        #endregion

        #region === RTT 与 ACK 处理 ===

        /// <summary>
        /// 获取下一个完整消息的大小（内部实现）。
        /// </summary>
        private int PeekSizeInternal()
        {
            if (_rcv_queue.Count == 0)
                return -1;

            var seg = _rcv_queue.First;
            if (seg == null)
                return -1;

            if (_stream != 0)
            {
                int length = 0;
                var node = _rcv_queue.FirstNode;
                while (node != null)
                {
                    length += (int)node.Value.Len;
                    node = node.Next;
                }

                return length;
            }
            else
            {
                if (seg.Frg == 0)
                    return (int)seg.Len;

                if (_rcv_queue.Count < seg.Frg + 1)
                    return -1;

                int length = 0;
                var node = _rcv_queue.FirstNode;
                for (int i = 0; i < seg.Frg + 1; i++)
                {
                    if (node == null)
                        return -1;
                    length += (int)node.Value.Len;
                    node = node.Next;
                }

                return length;
            }
        }

        /// <summary>
        /// 更新 RTT 估计值。收到 ACK 时调用。
        /// 对应 ikcp.c 的 ikcp_update_ack。
        /// </summary>
        private void UpdateAck(uint sn, uint ts)
        {
            // 只处理 sn >= _snd_una 的有效 ACK
            if (IsTimeLess(sn, _snd_una) || IsTimeLess(_snd_nxt, sn))
                return;

            // 计算本次 RTT
            int rtt = (int)(_current - ts);

            if (rtt < 0)
                rtt = 0;

            if (_rx_srtt == 0)
            {
                // 首次测量
                _rx_srtt = rtt;
                _rx_rttval = rtt / 2;
            }
            else
            {
                // 平滑 RTT 估计 (RFC 6298)
                int delta = rtt - _rx_srtt;
                if (delta < 0)
                    delta = -delta;

                _rx_rttval = (3 * _rx_rttval + delta) / 4;
                _rx_srtt = (7 * _rx_srtt + rtt) / 8;

                if (_rx_srtt < 1)
                    _rx_srtt = 1;
            }

            // 计算 RTO
            int rto = _rx_srtt + Math.Max(_rx_rttval * 4, KcpConstants.IKCP_RTO_MIN);
            _rx_rto = Math.Min(Math.Max(rto, _rx_minrto), KcpConstants.IKCP_RTO_MAX);
        }

        #endregion

        #region === 内部解析方法 ===

        private void ParseUna(uint una)
        {
            while (_snd_buf.Count > 0)
            {
                var seg = _snd_buf.First;
                if (seg == null)
                    break;

                if (IsTimeLess(seg.Sn, una))
                {
                    _snd_buf.RemoveFirst();
                    _segmentManager.Return(seg);
                }
                else
                {
                    break;
                }
            }
        }

        private void ParseAck(uint sn)
        {
            if (IsTimeLess(sn, _snd_una) || IsTimeLess(_snd_nxt, sn))
                return;

            var node = _snd_buf.FirstNode;
            while (node != null)
            {
                var seg = node.Value;
                if (sn == seg.Sn)
                {
                    _snd_buf.Remove(node);
                    _segmentManager.Return(seg);
                    break;
                }

                if (IsTimeLess(sn, seg.Sn))
                    break;

                node = node.Next;
            }
        }

        private void ParseFastack(uint sn, uint rcvNxt)
        {
            if (IsTimeLess(sn, _snd_una) || IsTimeLess(_snd_nxt, sn))
                return;

            var node = _snd_buf.FirstNode;
            while (node != null)
            {
                var seg = node.Value;
                if (IsTimeLess(sn, seg.Sn))
                    break;

                if (sn != seg.Sn)
                {
                    if (_fastlimit > 0 && seg.Fastack < _fastlimit)
                        seg.Fastack++;
                    else if (_fastlimit == 0)
                        seg.Fastack++;
                }

                node = node.Next;
            }
        }

        private void ParseData(KcpSegment newseg, ReadOnlySpan<byte> data)
        {
            data.CopyTo(newseg.Data);

            if (IsTimeLess(_rcv_nxt + _rmt_wnd, newseg.Sn))
            {
                _segmentManager.Return(newseg);
                return;
            }

            // 在 rcv_buf 中按 sn 降序寻找插入位置
            var node = _rcv_buf.LastNode;
            while (node != null)
            {
                var seg = node.Value;
                if (seg.Sn == newseg.Sn)
                {
                    _segmentManager.Return(newseg);
                    return;
                }

                if (IsTimeLess(seg.Sn, newseg.Sn))
                    break;

                node = node.Previous;
            }

            if (node != null)
                _rcv_buf.AddAfter(node, newseg);
            else
                _rcv_buf.AddFirst(newseg);

            MoveRcvBufToQueue();
        }

        private void MoveRcvBufToQueue()
        {
            while (_rcv_buf.Count > 0)
            {
                var seg = _rcv_buf.First;
                if (seg == null)
                    break;

                if (seg.Sn == _rcv_nxt && _rcv_queue.Count < _rmt_wnd)
                {
                    _rcv_buf.RemoveFirst();
                    _rcv_queue.AddLast(seg);
                    _rcv_nxt++;
                }
                else
                {
                    break;
                }
            }
        }

        #endregion

        #region === Flush ===

        private void Flush()
        {
            if (_state != KcpConstants.IKCP_STATE_AVAILABLE)
                return;

            // 首次 Flush 时初始化 cwnd
            if (_cwnd == 0 && _rmt_wnd > 0)
                _cwnd = 1;

            // 计算发送窗口（拥塞窗口 + 远端窗口）
            uint window = Math.Min(_cwnd, _rmt_wnd);

            // 滑动窗口：将 snd_queue 中的段移到 snd_buf
            while (IsTimeLess(_snd_nxt, _snd_una + window))
            {
                if (_snd_queue.Count == 0)
                    break;

                var seg = _snd_queue.First;
                _snd_queue.RemoveFirst();

                seg.Conv = _conv;
                seg.Wnd = (ushort)GetWndUnused();
                seg.Ts = _current;
                seg.Sn = _snd_nxt;
                seg.Una = _rcv_nxt;
                seg.Resendts = 0;
                seg.Rto = (uint)_rx_rto;
                seg.Fastack = 0;
                seg.Xmit = 0;

                _snd_buf.AddLast(seg);
                _snd_nxt++;
            }

            // 计算窗口探测
            if (_rmt_wnd == 0 && _probe_wait == 0)
            {
                _probe_wait = KcpConstants.IKCP_PROBE_INIT;
                _ts_probe = _current + _probe_wait;
            }
            else if (_rmt_wnd == 0)
            {
                if (IsTimeLess(_ts_probe, _current) || _current == _ts_probe)
                {
                    if (_probe_wait < KcpConstants.IKCP_PROBE_LIMIT)
                        _probe_wait += _probe_wait;  // 指数退避
                    else
                        _probe_wait = KcpConstants.IKCP_PROBE_LIMIT;

                    _ts_probe = _current + _probe_wait;
                    _probe |= (uint)KcpConstants.IKCP_PROBE_ASK;
                }
            }
            else
            {
                _ts_probe = 0;
                _probe_wait = 0;
            }

            int offset = 0;

            // 1. 发送窗口探测请求/响应
            if ((_probe & (uint)KcpConstants.IKCP_PROBE_ASK) != 0)
            {
                if (offset + KcpConstants.IKCP_OVERHEAD > _buffer.Length)
                {
                    Output?.Invoke(new Memory<byte>(_buffer, 0, offset));
                    offset = 0;
                }

                offset = EncodeSeg(_buffer, offset, _conv, KcpConstants.IKCP_CMD_WASK, 0, (ushort)GetWndUnused(), 0, 0, _rcv_nxt, 0);
            }

            _probe = 0;

            // 2. 发送 ACK 列表
            for (int i = 0; i < _acklist.Count; i++)
            {
                if (offset + KcpConstants.IKCP_OVERHEAD > _buffer.Length)
                {
                    Output?.Invoke(new Memory<byte>(_buffer, 0, offset));
                    offset = 0;
                }

                var (sn, ts) = _acklist[i];
                offset = EncodeSeg(_buffer, offset, _conv, KcpConstants.IKCP_CMD_ACK, 0, (ushort)GetWndUnused(), ts, sn, _rcv_nxt, 0);
            }

            _acklist.Clear();

            // 3. 发送数据段
            bool resent = false;
            var node = _snd_buf.FirstNode;
            while (node != null)
            {
                var seg = node.Value;
                bool needsend = false;

                if (seg.Xmit == 0)
                {
                    // 首次发送
                    needsend = true;
                    seg.Xmit++;
                    seg.Rto = (uint)_rx_rto;
                    seg.Resendts = _current + seg.Rto + _interval;
                }
                else if (_fastresend > 0 && seg.Fastack >= _fastresend)
                {
                    // 快速重传
                    needsend = true;
                    seg.Xmit++;
                    seg.Fastack = 0;
                    resent = true;
                }
                else if (seg.Resendts != 0 && IsTimeLess(seg.Resendts, _current))
                {
                    // 超时重传
                    needsend = true;
                    seg.Xmit++;
                    _xmit++;

                    if (_nodelay)
                    {
                        seg.Rto += (uint)_rx_rto;
                    }
                    else
                    {
                        seg.Rto += (uint)(_rx_rto / 2);
                    }

                    seg.Resendts = _current + seg.Rto;
                }

                if (needsend)
                {
                    seg.Wnd = (ushort)GetWndUnused();
                    seg.Una = _rcv_nxt;

                    int totalLen = KcpConstants.IKCP_OVERHEAD + (int)seg.Len;

                    if (offset + totalLen > _buffer.Length)
                    {
                        Output?.Invoke(new Memory<byte>(_buffer, 0, offset));
                        offset = 0;
                    }

                    offset = EncodeSeg(_buffer, offset, seg.Conv, seg.Cmd, seg.Frg, seg.Wnd, seg.Ts, seg.Sn, seg.Una, seg.Len);

                    if (seg.Len > 0)
                    {
                        Array.Copy(seg.Data, 0, _buffer, offset, (int)seg.Len);
                        offset += (int)seg.Len;
                    }

                    if (seg.Xmit >= KcpConstants.IKCP_DEADLINK)
                    {
                        _state = KcpConstants.IKCP_STATE_DEAD;
                    }
                }

                node = node.Next;
            }

            // 发送剩余缓冲
            if (offset > 0)
            {
                Output?.Invoke(new Memory<byte>(_buffer, 0, offset));
            }

            // 更新拥塞窗口
            if (_nocwnd == false)
            {
                if (resent)
                {
                    // 快速重传触发拥塞避免
                    uint inflight = _snd_nxt - _snd_una;
                    _ssthresh = Math.Max(inflight / 2, (uint)KcpConstants.IKCP_THRESH_MIN);
                    _cwnd = _ssthresh;
                }
                else
                {
                    // 正常增长
                    if (_cwnd < _ssthresh)
                    {
                        _cwnd += (uint)KcpConstants.IKCP_CWND_INCR;
                    }
                    else
                    {
                        if (_cwnd > 0)
                            _cwnd += (uint)Math.Max(1, KcpConstants.IKCP_CWND_INCR * KcpConstants.IKCP_CWND_INCR / (int)_cwnd);
                    }

                    if (_cwnd > _rmt_wnd)
                        _cwnd = _rmt_wnd;
                }
            }
        }

        private uint GetWndUnused()
        {
            if (_rcv_queue.Count < _rmt_wnd)
                return _rmt_wnd - (uint)_rcv_queue.Count;
            return 0;
        }

        #endregion

        #region === 编码 ===

        private static int EncodeSeg(byte[] buffer, int offset, uint conv, byte cmd, byte frg, ushort wnd, uint ts, uint sn, uint una, uint len)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), conv);
            buffer[offset + 4] = cmd;
            buffer[offset + 5] = frg;
            BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(offset + 6), wnd);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset + 8), ts);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset + 12), sn);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset + 16), una);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset + 20), len);
            return offset + KcpConstants.IKCP_OVERHEAD;
        }

        #endregion

        #region === 时间比较 ===

        /// <summary>
        /// 判断 a 是否小于 b（处理 uint 回绕）。
        /// 对应 ikcp.c 中的 _itimediff((int)(a - b)) &lt; 0。
        /// </summary>
        private static bool IsTimeLess(uint a, uint b)
        {
            return (int)(a - b) < 0;
        }

        #endregion

        #region === 辅助类 ===

        /// <summary>
        /// ACK 记录列表。
        /// </summary>
        private class KcpAckList
        {
            private readonly List<(uint Sn, uint Ts)> _items = new List<(uint Sn, uint Ts)>();

            public int Count => _items.Count;

            public void Add(uint sn, uint ts)
            {
                _items.Add((sn, ts));
            }

            public (uint Sn, uint Ts) this[int index] => _items[index];

            public void Clear()
            {
                _items.Clear();
            }
        }

        /// <summary>
        /// KCP 数据段双向链表。
        /// </summary>
        private class KcpSegmentList
        {
            private readonly LinkedList<KcpSegment> _list = new LinkedList<KcpSegment>();

            public int Count => _list.Count;

            public KcpSegment First => _list.First?.Value;

            public KcpSegment Last => _list.Last?.Value;

            public LinkedListNode<KcpSegment> FirstNode => _list.First;

            public LinkedListNode<KcpSegment> LastNode => _list.Last;

            public void AddFirst(KcpSegment seg) => _list.AddFirst(seg);

            public void AddLast(KcpSegment seg) => _list.AddLast(seg);

            public void AddAfter(LinkedListNode<KcpSegment> node, KcpSegment seg) => _list.AddAfter(node, seg);

            public void RemoveFirst()
            {
                if (_list.First != null)
                    _list.RemoveFirst();
            }

            public void Remove(LinkedListNode<KcpSegment> node) => _list.Remove(node);
        }

        #endregion
    }
}
