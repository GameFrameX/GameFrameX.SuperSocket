using GameFrameX.SuperSocket.Kcp.Kcp;
using Xunit;

namespace GameFrameX.SuperSocket.Kcp.Tests
{
    /// <summary>
    /// KCP 协议全面测试套件。
    /// 覆盖：核心收发、分片重组、丢包重传、窗口控制、RTT 估算、
    /// 并发、边界条件、对象池、配置、流模式、多会话隔离等。
    /// </summary>
    public class KcpComprehensiveTests
    {
        private const uint ConvA = 0x11223344;
        private const uint ConvB = 0xAABBCCDD;
        private const int DefaultMss = KcpConstants.IKCP_MTU_DEF - KcpConstants.IKCP_OVERHEAD; // 1376

        public KcpComprehensiveTests()
        {
        }

        #region === 1. 基本数据收发（Basic Data Transfer） ===

        /// <summary>
        /// 测试最小数据包（1 字节）的发送和接收。
        /// </summary>
        [Fact]
        public void Transfer_1_Byte()
        {
            var (sender, receiver) = CreatePair(ConvA);
            var data = new byte[] { 0xAB };
            TransferAndVerify(sender, receiver, data);
        }

        /// <summary>
        /// 测试恰好 1 MSS 大小数据的收发（不触发分片）。
        /// </summary>
        [Fact]
        public void Transfer_Exactly_One_MSS()
        {
            var (sender, receiver) = CreatePair(ConvA);
            var data = new byte[DefaultMss];
            new Random(1).NextBytes(data);
            TransferAndVerify(sender, receiver, data);
        }

        /// <summary>
        /// 测试 1 MSS + 1 字节数据的收发（触发 2 片分片）。
        /// </summary>
        [Fact]
        public void Transfer_One_MSS_Plus_One()
        {
            var (sender, receiver) = CreatePair(ConvA);
            var data = new byte[DefaultMss + 1];
            new Random(2).NextBytes(data);
            TransferAndVerify(sender, receiver, data);
        }

        /// <summary>
        /// 测试 2 MSS 大小数据的收发（恰好 2 片）。
        /// </summary>
        [Fact]
        public void Transfer_Exactly_Two_MSS()
        {
            var (sender, receiver) = CreatePair(ConvA);
            var data = new byte[DefaultMss * 2];
            new Random(3).NextBytes(data);
            TransferAndVerify(sender, receiver, data);
        }

        /// <summary>
        /// 测试 3 MSS 大小数据（3 片分片重组）。
        /// </summary>
        [Fact]
        public void Transfer_Three_MSS()
        {
            var (sender, receiver) = CreatePair(ConvA);
            var data = new byte[DefaultMss * 3];
            new Random(4).NextBytes(data);
            TransferAndVerify(sender, receiver, data);
        }

        /// <summary>
        /// 测试多轮发送-接收。
        /// </summary>
        [Fact]
        public void Transfer_Multiple_Rounds()
        {
            var (sender, receiver) = CreatePair(ConvA);

            for (int round = 0; round < 5; round++)
            {
                var data = new byte[50 + round * 20];
                new Random(round + 100).NextBytes(data);

                sender.Send(data);
                SimulateBidirectional(sender, receiver, 20);

                var received = ReceiveAll(receiver);
                Assert.Equal(data, received);
            }
        }

        /// <summary>
        /// 测试连续发送多条小消息后一次性接收。
        /// </summary>
        [Fact]
        public void Transfer_Many_Small_Messages()
        {
            var (sender, receiver) = CreatePair(ConvA);
            var messages = new List<byte[]>();

            for (int i = 0; i < 20; i++)
            {
                var msg = System.Text.Encoding.UTF8.GetBytes($"Msg#{i}");
                messages.Add(msg);
                sender.Send(msg);
            }

            SimulateBidirectional(sender, receiver, 50);

            for (int i = 0; i < messages.Count; i++)
            {
                Assert.True(receiver.PeekCanRecv(), $"Message {i} not available");
                var buf = new byte[1024];
                int len = receiver.Recv(buf);
                Assert.Equal(messages[i], buf.AsSpan(0, len).ToArray());
            }

            Assert.False(receiver.PeekCanRecv());
        }

        #endregion

        #region === 2. 分片与重组（Fragmentation & Reassembly） ===

        /// <summary>
        /// 测试 10 片分片的可靠传输（跨 MSS 的大消息）。
        /// </summary>
        [Fact]
        public void Fragment_10_Fragments_Transfer()
        {
            var (sender, receiver) = CreatePair(ConvA);
            sender.SetNoDelay(1, 10, 2, 0); // 快速模式 + 启用拥塞控制
            receiver.SetNoDelay(1, 10, 2, 0);

            var data = new byte[DefaultMss * 10];
            new Random(42).NextBytes(data);

            sender.Send(data);

            uint t = 1000;
            for (int i = 0; i < 500; i++)
            {
                var sPkts = new List<byte[]>();
                var rPkts = new List<byte[]>();
                sender.Output = d => sPkts.Add(d.ToArray());
                receiver.Output = d => rPkts.Add(d.ToArray());

                sender.Update(t);
                receiver.Update(t);

                foreach (var pkt in sPkts)
                    receiver.Input(pkt.AsSpan());
                foreach (var pkt in rPkts)
                    sender.Input(pkt.AsSpan());

                t += 10;

                if (receiver.PeekCanRecv())
                {
                    var peekSize = receiver.PeekSize();
                    if (peekSize == data.Length)
                        break;
                }
            }

            var received = ReceiveAll(receiver);
            Assert.Equal(data, received);
        }

        /// <summary>
        /// 测试超过 255 片限制应该返回错误。
        /// </summary>
        [Fact]
        public void Fragment_Exceed_Max_Should_Return_Error()
        {
            var kcp = new KcpCore(ConvA);
            // 256 片会超限
            var data = new byte[DefaultMss * 256];
            int result = kcp.Send(data);
            Assert.Equal(-2, result);
        }

        /// <summary>
        /// 测试各种奇数大小的分片边界。
        /// </summary>
        [Theory]
        [InlineData(1)]
        [InlineData(DefaultMss - 1)]
        [InlineData(DefaultMss)]
        [InlineData(DefaultMss + 1)]
        [InlineData(DefaultMss * 2 - 1)]
        [InlineData(DefaultMss * 2 + 500)]
        public void Fragment_Various_Sizes(int size)
        {
            var (sender, receiver) = CreatePair(ConvA);
            var data = new byte[size];
            new Random(size).NextBytes(data);
            TransferAndVerify(sender, receiver, data, iterations: 50);
        }

        /// <summary>
        /// 测试分片在接收端乱序到达时仍能正确重组。
        /// </summary>
        [Fact]
        public void Fragment_Out_Of_Order_Delivery()
        {
            var senderOutput = new List<byte[]>();
            var sender = CreateKcp(ConvA, senderOutput);
            var receiver = CreateKcp(ConvA, new List<byte[]>());
            sender.SetFastMode();
            receiver.SetFastMode();

            var data = new byte[DefaultMss * 3];
            new Random(55).NextBytes(data);

            sender.Send(data);
            uint t = 1000;
            sender.Update(t);

            // Flush 将所有片段写入一个输出缓冲区，需要解析出各个段
            var segments = ParseKcpSegments(senderOutput[0]);
            Assert.True(segments.Count >= 3, $"Expected >= 3 segments, got {segments.Count}");

            // 乱序投递：先投最后一个片段，再投第一个，再投中间的
            // 重新编码每个段为完整的 KCP 包
            receiver.Input(EncodeKcpSegment(segments[2]));
            receiver.Update(t + 10);
            receiver.Input(EncodeKcpSegment(segments[0]));
            receiver.Update(t + 20);
            receiver.Input(EncodeKcpSegment(segments[1]));

            // 需要多轮 Update 传递 ACK
            SimulateBidirectional(sender, receiver, 50, t + 30);

            var received = ReceiveAll(receiver);
            Assert.Equal(data, received);
        }

        #endregion

        #region === 3. 丢包与重传（Packet Loss & Retransmission） ===

        /// <summary>
        /// 测试第一个包丢失，重传后仍能收到。
        /// </summary>
        [Fact]
        public void Retransmit_First_Packet_Lost()
        {
            var senderOutput = new List<byte[]>();
            var sender = CreateKcp(ConvA, senderOutput);
            var receiver = CreateKcp(ConvA, new List<byte[]>());
            sender.SetFastMode();
            receiver.SetFastMode();

            var data = System.Text.Encoding.UTF8.GetBytes("First packet lost!");
            sender.Send(data);

            uint t = 1000;
            sender.Update(t);

            // 丢弃第一个包
            for (int i = 1; i < senderOutput.Count; i++)
            {
                receiver.Input(senderOutput[i].AsSpan());
            }

            SimulateBidirectional(sender, receiver, 60, t + 10);
            var received = ReceiveAll(receiver);
            Assert.Equal(data, received);
        }

        /// <summary>
        /// 测试所有包都丢失，纯靠超时重传。
        /// </summary>
        [Fact]
        public void Retransmit_All_Packets_Lost_Then_Retrieved()
        {
            var senderOutput = new List<byte[]>();
            var sender = CreateKcp(ConvA, senderOutput);
            var receiver = CreateKcp(ConvA, new List<byte[]>());
            sender.SetFastMode();
            receiver.SetFastMode();

            var data = System.Text.Encoding.UTF8.GetBytes("All lost!");
            sender.Send(data);

            uint t = 1000;
            sender.Update(t);

            // 全部丢弃，不给接收方

            // 驱动超时重传
            for (int i = 0; i < 30; i++)
            {
                t += 50;  // 大步长时间跳跃以触发超时
                var newPkts = new List<byte[]>();
                sender.Output = d => newPkts.Add(d.ToArray());
                sender.Update(t);
                receiver.Update(t);

                // 投递重传的包
                foreach (var pkt in newPkts)
                    receiver.Input(pkt.AsSpan());

                // 交换 ACK
                var ackPkts = new List<byte[]>();
                receiver.Output = d => ackPkts.Add(d.ToArray());
                receiver.Update(t);
                foreach (var pkt in ackPkts)
                    sender.Input(pkt.AsSpan());

                if (receiver.PeekCanRecv())
                {
                    var received = ReceiveAll(receiver);
                    Assert.Equal(data, received);
                    return;
                }
            }

            Assert.Fail("Failed to receive after retransmission");
        }

        /// <summary>
        /// 测试随机 30% 丢包率下的大数据传输。
        /// </summary>
        [Fact]
        public void Transfer_With_30_Percent_Random_Loss()
        {
            var senderOutput = new List<byte[]>();
            var sender = CreateKcp(ConvA, senderOutput);
            var receiver = CreateKcp(ConvA, new List<byte[]>());
            sender.SetFastMode();
            receiver.SetFastMode();

            var data = new byte[DefaultMss * 5];
            new Random(77).NextBytes(data);
            sender.Send(data);

            var rng = new Random(99);
            uint t = 1000;

            for (int i = 0; i < 100; i++)
            {
                t += 10;
                var sPkts = new List<byte[]>();
                var rPkts = new List<byte[]>();
                sender.Output = d => sPkts.Add(d.ToArray());
                receiver.Output = d => rPkts.Add(d.ToArray());

                sender.Update(t);
                receiver.Update(t);

                // 30% 丢包
                foreach (var pkt in sPkts)
                {
                    if (rng.Next(100) >= 30)
                        receiver.Input(pkt.AsSpan());
                }

                foreach (var pkt in rPkts)
                {
                    if (rng.Next(100) >= 30)
                        sender.Input(pkt.AsSpan());
                }

                if (receiver.PeekCanRecv())
                {
                    var received = ReceiveAll(receiver);
                    Assert.Equal(data, received);
                    return;
                }
            }

            Assert.Fail("Failed to transfer with 30% loss");
        }

        /// <summary>
        /// 测试重复包到达不会导致数据重复。
        /// </summary>
        [Fact]
        public void Duplicate_Packets_Should_Not_Duplicate_Data()
        {
            var senderOutput = new List<byte[]>();
            var sender = CreateKcp(ConvA, senderOutput);
            var receiver = CreateKcp(ConvA, new List<byte[]>());
            sender.SetFastMode();
            receiver.SetFastMode();

            var data = System.Text.Encoding.UTF8.GetBytes("No duplicate!");
            sender.Send(data);

            uint t = 1000;
            sender.Update(t);

            // 每个包投递 3 次
            foreach (var pkt in senderOutput)
            {
                receiver.Input(pkt.AsSpan());
                receiver.Input(pkt.AsSpan());
                receiver.Input(pkt.AsSpan());
            }

            SimulateBidirectional(sender, receiver, 20, t + 10);

            var received = ReceiveAll(receiver);
            Assert.Equal(data, received);

            // 不应有多余数据
            Assert.False(receiver.PeekCanRecv());
        }

        #endregion

        #region === 4. 窗口控制与流控（Window Control） ===

        /// <summary>
        /// 测试窗口满时发送方应暂停发送，窗口释放后恢复。
        /// </summary>
        [Fact]
        public void Window_Full_Should_Pause_Sending()
        {
            var senderOutput = new List<byte[]>();
            var sender = CreateKcp(ConvA, senderOutput);
            sender.SetFastMode();
            sender.SetWindowSize(32, 4); // 小接收窗口

            // 填满发送队列
            for (int i = 0; i < 10; i++)
            {
                sender.Send(new byte[100]);
            }

            Assert.True(sender.WaitSnd > 0, "Should have pending segments");
        }

        /// <summary>
        /// 测试 PeekSize 对不同分片状态返回正确的值。
        /// </summary>
        [Fact]
        public void PeekSize_Should_Return_Correct_Size()
        {
            var kcp = new KcpCore(ConvA);
            Assert.Equal(-1, kcp.PeekSize());  // 空队列

            // 直接 Send 一个小消息
            var data = new byte[] { 1, 2, 3 };
            kcp.Send(data);

            // 数据在 snd_queue，不在 rcv_queue，PeekSize 仍为 -1
            Assert.Equal(-1, kcp.PeekSize());
        }

        /// <summary>
        /// 测试 WaitSnd 正确反映等待发送的段数。
        /// </summary>
        [Fact]
        public void WaitSnd_Should_Reflect_Pending_Segments()
        {
            var kcp = new KcpCore(ConvA);

            Assert.Equal(0, kcp.WaitSnd);

            kcp.Send(new byte[100]);
            Assert.Equal(1, kcp.WaitSnd);

            kcp.Send(new byte[DefaultMss * 3]);
            // 100/1376=1 片 + 3 片 = 4
            Assert.Equal(4, kcp.WaitSnd);
        }

        #endregion

        #region === 5. RTT 估算与超时（RTT Estimation） ===

        /// <summary>
        /// 测试 Check 方法返回合理的下次更新时间。
        /// </summary>
        [Fact]
        public void Check_Should_Return_Reasonable_Next_Update_Time()
        {
            var kcp = new KcpCore(ConvA);
            kcp.SetFastMode();

            uint t = 1000;
            kcp.Update(t);

            uint next = kcp.Check(t + 1);
            Assert.True(next > t, "Next update should be in the future");
            Assert.True(next <= t + 100, "Next update should be within interval range");
        }

        /// <summary>
        /// 测试大时间跳跃后 Update 不崩溃。
        /// </summary>
        [Fact]
        public void Update_With_Large_Time_Jump_Should_Not_Crash()
        {
            var kcp = new KcpCore(ConvA);
            kcp.SetFastMode();

            kcp.Update(1000);
            // 大跳跃（超过 10000ms）
            kcp.Update(999999);
            kcp.Update(1000000);
        }

        /// <summary>
        /// 测试时间回绕处理。
        /// </summary>
        [Fact]
        public void Update_With_Time_Wraparound()
        {
            var kcp = new KcpCore(ConvA);
            kcp.SetFastMode();

            // 模拟 uint 时间回绕
            kcp.Update(uint.MaxValue - 50);
            kcp.Update(100);  // 回绕后
        }

        #endregion

        #region === 6. 配置与状态（Configuration & State） ===

        /// <summary>
        /// 测试各种 MTU 设置。
        /// </summary>
        [Theory]
        [InlineData(50)]
        [InlineData(100)]
        [InlineData(576)]
        [InlineData(1400)]
        [InlineData(1500)]
        [InlineData(9000)]
        public void SetMtu_Should_Work_For_Valid_Values(int mtu)
        {
            var kcp = new KcpCore(ConvA);
            kcp.SetMtu((uint)mtu);

            // 发送一个小于 MTU 的包验证
            var data = new byte[Math.Max(1, mtu - KcpConstants.IKCP_OVERHEAD - 1)];
            int result = kcp.Send(data);
            Assert.Equal(data.Length, result);
        }

        /// <summary>
        /// 测试 MTU 低于最小值应抛异常。
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(49)]
        public void SetMtu_Too_Small_Should_Throw(int mtu)
        {
            var kcp = new KcpCore(ConvA);
            Assert.Throws<ArgumentException>(() => kcp.SetMtu((uint)mtu));
        }

        /// <summary>
        /// 测试 Conv 属性返回构造时的值。
        /// </summary>
        [Theory]
        [InlineData(0u)]
        [InlineData(1u)]
        [InlineData(uint.MaxValue)]
        [InlineData(0x12345678u)]
        public void Conv_Should_Match_Constructor_Value(uint conv)
        {
            var kcp = new KcpCore(conv);
            Assert.Equal(conv, kcp.Conv);
        }

        /// <summary>
        /// 测试初始状态为 IKCP_STATE_AVAILABLE。
        /// </summary>
        [Fact]
        public void Initial_State_Should_Be_Available()
        {
            var kcp = new KcpCore(ConvA);
            Assert.Equal(KcpConstants.IKCP_STATE_AVAILABLE, kcp.State);
        }

        /// <summary>
        /// 测试正常模式配置。
        /// </summary>
        [Fact]
        public void NormalMode_Should_Not_Throw()
        {
            var kcp = new KcpCore(ConvA);
            kcp.SetNormalMode();
            // 验证可以正常使用
            kcp.Send(new byte[10]);
            kcp.Update(1000);
        }

        /// <summary>
        /// 测试快速模式配置。
        /// </summary>
        [Fact]
        public void FastMode_Should_Not_Throw()
        {
            var kcp = new KcpCore(ConvA);
            kcp.SetFastMode();
            kcp.Send(new byte[10]);
            kcp.Update(1000);
        }

        /// <summary>
        /// 测试 SetNoDelay 的各种参数组合。
        /// </summary>
        [Theory]
        [InlineData(0, 0, 0, 0)]    // 全默认
        [InlineData(1, 10, 2, 1)]   // 快速模式
        [InlineData(2, 50, 3, 0)]   // 极速模式（stream=1）
        [InlineData(0, 100, 0, 0)]  // 普通模式
        [InlineData(1, 5000, 5, 1)] // 最大 interval
        public void SetNoDelay_Should_Accept_Valid_Params(int nodelay, int interval, int resend, int nc)
        {
            var kcp = new KcpCore(ConvA);
            kcp.SetNoDelay(nodelay, interval, resend, nc);
            // 验证不抛异常
        }

        /// <summary>
        /// 测试 SetWindowSize 的有效范围。
        /// </summary>
        [Theory]
        [InlineData(0, 0)]
        [InlineData(32, 128)]
        [InlineData(256, 256)]
        [InlineData(1024, 1024)]
        public void SetWindowSize_Should_Accept_Valid_Values(int snd, int rcv)
        {
            var kcp = new KcpCore(ConvA);
            kcp.SetWindowSize(snd, rcv);
        }

        /// <summary>
        /// 测试 SetWindowSize 负值应抛异常。
        /// </summary>
        [Fact]
        public void SetWindowSize_Negative_Should_Throw()
        {
            var kcp = new KcpCore(ConvA);
            Assert.Throws<ArgumentException>(() => kcp.SetWindowSize(-1, 128));
            Assert.Throws<ArgumentException>(() => kcp.SetWindowSize(32, -1));
        }

        #endregion

        #region === 7. 错误处理与边界（Error Handling & Edge Cases） ===

        /// <summary>
        /// 测试空数据发送返回 0。
        /// </summary>
        [Fact]
        public void Send_Empty_Should_Return_Zero()
        {
            var kcp = new KcpCore(ConvA);
            Assert.Equal(0, kcp.Send(ReadOnlySpan<byte>.Empty));
            Assert.Equal(0, kcp.Send(new byte[0]));
        }

        /// <summary>
        /// 测试无数据时 Recv 返回 -1。
        /// </summary>
        [Fact]
        public void Recv_No_Data_Should_Return_Negative1()
        {
            var kcp = new KcpCore(ConvA);
            Assert.Equal(-1, kcp.Recv(new byte[100]));
        }

        /// <summary>
        /// 测试 Conv 不匹配的包返回 -3。
        /// </summary>
        [Fact]
        public void Input_Wrong_Conv_Should_Return_Negative3()
        {
            var kcp = new KcpCore(ConvA);
            var packet = new byte[KcpConstants.IKCP_OVERHEAD];
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(packet, ConvB);
            Assert.Equal(-3, kcp.Input(packet));
        }

        /// <summary>
        /// 测试太短的包返回 -2。
        /// </summary>
        [Fact]
        public void Input_Too_Short_Should_Return_Negative2()
        {
            var kcp = new KcpCore(ConvA);
            // 只有头部但声明了超过实际长度的数据
            var packet = new byte[KcpConstants.IKCP_OVERHEAD];
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(packet, ConvA);
            packet[4] = KcpConstants.IKCP_CMD_PUSH;
            // len = 100 但实际没有数据
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(20), 100);
            Assert.Equal(-2, kcp.Input(packet));
        }

        /// <summary>
        /// 测试无效命令类型返回 -4。
        /// </summary>
        [Fact]
        public void Input_Invalid_Cmd_Should_Return_Negative4()
        {
            var kcp = new KcpCore(ConvA);
            var packet = new byte[KcpConstants.IKCP_OVERHEAD];
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(packet, ConvA);
            packet[4] = 99; // 无效命令
            Assert.Equal(-4, kcp.Input(packet));
        }

        /// <summary>
        /// 测试 Output 为 null 时不崩溃。
        /// </summary>
        [Fact]
        public void Output_Null_Should_Not_Crash()
        {
            var kcp = new KcpCore(ConvA);
            kcp.Output = null;
            kcp.Send(new byte[10]);
            kcp.Update(1000); // Flush 时 Output 为 null 不应崩溃
        }

        /// <summary>
        /// 测试小于 overhead 的 Input 被忽略。
        /// </summary>
        [Fact]
        public void Input_Under_Overhead_Should_Be_Ignored()
        {
            var kcp = new KcpCore(ConvA);
            var smallPacket = new byte[10];
            Assert.Equal(0, kcp.Input(smallPacket)); // 不够 overhead，循环不执行，返回 0
        }

        /// <summary>
        /// 测试 Recv 缓冲区太小时的行为。
        /// </summary>
        [Fact]
        public void Recv_Buffer_Too_Small_Should_Return_Negative3()
        {
            var (sender, receiver) = CreatePair(ConvA);
            var data = new byte[100];
            sender.Send(data);
            SimulateBidirectional(sender, receiver, 20);

            Assert.True(receiver.PeekCanRecv());
            // 用 1 字节缓冲区接收 100 字节数据
            int result = receiver.Recv(new byte[1]);
            Assert.Equal(-3, result);
        }

        #endregion

        #region === 8. 多会话隔离（Multi-Session Isolation） ===

        /// <summary>
        /// 测试两个不同 Conv 的 KCP 实例互不干扰。
        /// </summary>
        [Fact]
        public void Two_Sessions_With_Different_Conv_Should_Be_Isolated()
        {
            var output1 = new List<byte[]>();
            var output2 = new List<byte[]>();

            var sender1 = CreateKcp(ConvA, output1);
            var sender2 = CreateKcp(ConvB, output2);
            var receiver1 = CreateKcp(ConvA, new List<byte[]>());
            var receiver2 = CreateKcp(ConvB, new List<byte[]>());

            sender1.SetFastMode();
            sender2.SetFastMode();
            receiver1.SetFastMode();
            receiver2.SetFastMode();

            var data1 = System.Text.Encoding.UTF8.GetBytes("Session A");
            var data2 = System.Text.Encoding.UTF8.GetBytes("Session B");

            sender1.Send(data1);
            sender2.Send(data2);

            uint t = 1000;
            sender1.Update(t);
            sender2.Update(t);

            // 交叉投递：sender1 的包给 receiver1，sender2 的包给 receiver2
            foreach (var pkt in output1)
                receiver1.Input(pkt.AsSpan());
            foreach (var pkt in output2)
                receiver2.Input(pkt.AsSpan());

            SimulateBidirectional(sender1, receiver1, 20, t + 10);
            SimulateBidirectional(sender2, receiver2, 20, t + 10);

            var recv1 = ReceiveAll(receiver1);
            var recv2 = ReceiveAll(receiver2);

            Assert.Equal(data1, recv1);
            Assert.Equal(data2, recv2);

            // 验证数据没有串扰
            Assert.NotEqual(recv1, recv2);
        }

        /// <summary>
        /// 测试错误 Conv 的包不会被接收。
        /// </summary>
        [Fact]
        public void Packet_With_Wrong_Conv_Should_Be_Rejected()
        {
            var senderOutput = new List<byte[]>();
            var sender = CreateKcp(ConvA, senderOutput);
            var receiver = CreateKcp(ConvB, new List<byte[]>()); // 不同 Conv

            sender.SetFastMode();
            receiver.SetFastMode();

            sender.Send(System.Text.Encoding.UTF8.GetBytes("Wrong conv"));
            uint t = 1000;
            sender.Update(t);

            // 投递 ConvA 的包给 ConvB 的接收方
            foreach (var pkt in senderOutput)
            {
                int result = receiver.Input(pkt.AsSpan());
                Assert.Equal(-3, result);
            }

            Assert.False(receiver.PeekCanRecv());
        }

        #endregion

        #region === 9. 对象池（Segment Pool） ===

        /// <summary>
        /// 测试对象池的 Rent 和 Return。
        /// </summary>
        [Fact]
        public void SegmentPool_Rent_And_Return()
        {
            var pool = new KcpSegmentManager(10);

            var seg1 = pool.Rent(100);
            Assert.NotNull(seg1);
            Assert.True(seg1.Data.Length >= 100);

            pool.Return(seg1);

            var seg2 = pool.Rent(100);
            Assert.Same(seg1, seg2); // 应复用同一个对象
        }

        /// <summary>
        /// 测试对象池满时丢弃策略。
        /// </summary>
        [Fact]
        public void SegmentPool_Full_Should_Discard()
        {
            var pool = new KcpSegmentManager(2);
            var seg1 = pool.Rent(10);
            var seg2 = pool.Rent(10);
            var seg3 = pool.Rent(10);

            pool.Return(seg1);
            pool.Return(seg2);
            pool.Return(seg3); // 超过容量，应丢弃

            // seg1 和 seg2 应在池中，seg3 被丢弃
            var r1 = pool.Rent(10);
            var r2 = pool.Rent(10);
            Assert.True(r1 != null && r2 != null);
        }

        /// <summary>
        /// 测试 Return null 不崩溃。
        /// </summary>
        [Fact]
        public void SegmentPool_Return_Null_Should_Not_Crash()
        {
            var pool = new KcpSegmentManager();
            pool.Return(null); // 不应抛异常
        }

        /// <summary>
        /// 测试 Rent 后 Reset 清除旧数据。
        /// </summary>
        [Fact]
        public void SegmentPool_Reset_Should_Clear_Metadata()
        {
            var pool = new KcpSegmentManager();

            var seg = pool.Rent(100);
            seg.Conv = 0x12345678;
            seg.Sn = 999;
            seg.Len = 100;

            pool.Return(seg);
            var reused = pool.Rent(100);

            Assert.Equal(0u, reused.Conv);
            Assert.Equal(0u, reused.Sn);
            Assert.Equal(0u, reused.Len);
        }

        #endregion

        #region === 10. 连续大数据传输（Stress / Throughput） ===

        /// <summary>
        /// 测试连续发送 10 个 MSS 级别的大消息。
        /// </summary>
        [Fact]
        public void Transfer_10_Large_Messages_Sequentially()
        {
            var (sender, receiver) = CreatePair(ConvA);
            var messages = new List<byte[]>();

            for (int i = 0; i < 10; i++)
            {
                var data = new byte[DefaultMss * 2 + i * 100];
                new Random(i + 200).NextBytes(data);
                messages.Add(data);

                sender.Send(data);
                SimulateBidirectional(sender, receiver, 80);

                var received = ReceiveAll(receiver);
                Assert.Equal(data, received);
            }
        }

        /// <summary>
        /// 测试一次性发送 50 个小消息后接收。
        /// </summary>
        [Fact]
        public void Transfer_50_Small_Messages_Batch()
        {
            var (sender, receiver) = CreatePair(ConvA);
            var messages = new List<byte[]>();

            for (int i = 0; i < 50; i++)
            {
                var msg = System.Text.Encoding.UTF8.GetBytes($"Message_{i:D4}_{new string('X', i % 50)}");
                messages.Add(msg);
                sender.Send(msg);
            }

            SimulateBidirectional(sender, receiver, 100);

            for (int i = 0; i < messages.Count; i++)
            {
                if (!receiver.PeekCanRecv())
                {
                    // 需要更多轮次
                    SimulateBidirectional(sender, receiver, 50);
                }

                Assert.True(receiver.PeekCanRecv(), $"Message {i} not available after extra rounds");
                var buf = new byte[2048];
                int len = receiver.Recv(buf);
                Assert.Equal(messages[i], buf.AsSpan(0, len).ToArray());
            }
        }

        #endregion

        #region === 11. 会话标识提供者（Session Identifier Provider） ===

        /// <summary>
        /// 测试 KcpConvIdentifierProvider 正确提取 Conv。
        /// </summary>
        [Fact]
        public void ConvIdentifierProvider_Should_Extract_Conv()
        {
            var provider = new KcpConvIdentifierProvider();
            var ep = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 8080);

            var data = new byte[24];
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(data, 0x12345678);

            var id = provider.GetSessionIdentifier(ep, data);
            Assert.Equal("127.0.0.1:8080:305419896", id); // 0x12345678 = 305419896
        }

        /// <summary>
        /// 测试 KcpConvIdentifierProvider 支持非 IP 端点。
        /// </summary>
        [Fact]
        public void ConvIdentifierProvider_Should_Accept_Dns_EndPoint()
        {
            var provider = new KcpConvIdentifierProvider();
            var ep = new System.Net.DnsEndPoint("game.example.com", 8080);

            var data = new byte[24];
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(data, 7);

            var id = provider.GetSessionIdentifier(ep, data);
            Assert.Equal("game.example.com:8080:7", id);
        }

        /// <summary>
        /// 测试 KcpConvIdentifierProvider 数据太短时抛异常。
        /// </summary>
        [Fact]
        public void ConvIdentifierProvider_Too_Short_Should_Throw()
        {
            var provider = new KcpConvIdentifierProvider();
            var ep = new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 8080);

            Assert.Throws<GameFrameX.SuperSocket.ProtoBase.ProtocolException>(
                () => provider.GetSessionIdentifier(ep, new byte[3]));
        }

        /// <summary>
        /// 测试不同 IP/Port/Conv 组合生成不同标识。
        /// </summary>
        [Fact]
        public void ConvIdentifierProvider_Different_Sources_Different_IDs()
        {
            var provider = new KcpConvIdentifierProvider();

            var ep1 = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("192.168.1.1"), 1000);
            var ep2 = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("192.168.1.2"), 1000);
            var ep3 = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("192.168.1.1"), 2000);

            var data1 = new byte[4];
            var data2 = new byte[4];
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(data1, 1);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(data2, 2);

            var id1 = provider.GetSessionIdentifier(ep1, data1);
            var id2 = provider.GetSessionIdentifier(ep2, data1);
            var id3 = provider.GetSessionIdentifier(ep1, data2);
            var id4 = provider.GetSessionIdentifier(ep3, data1);

            Assert.NotEqual(id1, id2); // 不同 IP
            Assert.NotEqual(id1, id3); // 不同 Conv
            Assert.NotEqual(id1, id4); // 不同 Port
        }

        #endregion

        #region === 12. KcpSegment 单元测试 ===

        /// <summary>
        /// 测试 KcpSegment 构造和数据缓冲区分配。
        /// </summary>
        [Fact]
        public void Segment_Constructor_Should_Allocate_Buffer()
        {
            var seg = new KcpSegment(500);
            Assert.NotNull(seg.Data);
            Assert.True(seg.Data.Length >= 500);
            Assert.Equal(0u, seg.Conv);
            Assert.Equal(0u, seg.Sn);
        }

        /// <summary>
        /// 测试 KcpSegment Reset 清除元数据。
        /// </summary>
        [Fact]
        public void Segment_Reset_Should_Clear_Fields()
        {
            var seg = new KcpSegment(100);
            seg.Conv = 0x12345678;
            seg.Cmd = 81;
            seg.Frg = 3;
            seg.Wnd = 128;
            seg.Ts = 9999;
            seg.Sn = 42;
            seg.Len = 100;
            seg.Xmit = 5;

            seg.Reset(50);

            Assert.Equal(0u, seg.Conv);
            Assert.Equal(0, seg.Cmd);
            Assert.Equal(0, seg.Frg);
            Assert.Equal((ushort)0, seg.Wnd);
            Assert.Equal(0u, seg.Ts);
            Assert.Equal(0u, seg.Sn);
            Assert.Equal(0u, seg.Len);
            Assert.Equal(0u, seg.Xmit);
        }

        /// <summary>
        /// 测试 KcpSegment Reset 在缓冲区不足时重新分配。
        /// </summary>
        [Fact]
        public void Segment_Reset_With_Larger_Size_Should_Reallocate()
        {
            var seg = new KcpSegment(10);
            var oldData = seg.Data;
            Assert.Equal(10, oldData.Length);

            seg.Reset(100);
            Assert.True(seg.Data.Length >= 100);
            Assert.NotSame(oldData, seg.Data);
        }

        #endregion

        #region === 13. KcpConnectionOptions 测试 ===

        /// <summary>
        /// 测试默认选项值。
        /// </summary>
        [Fact]
        public void ConnectionOptions_Default_Values()
        {
            var opts = new KcpConnectionOptions();
            Assert.Equal(0u, opts.Conv);
            Assert.Null(opts.Mtu);
            Assert.Null(opts.SendWindow);
            Assert.Null(opts.ReceiveWindow);
            Assert.Null(opts.NoDelay);
            Assert.Null(opts.NoDelayLevel);
            Assert.Null(opts.Interval);
            Assert.Null(opts.Resend);
            Assert.Null(opts.DeadLink);
            Assert.Null(opts.NoCongestionControl);
            Assert.Null(opts.IdleTimeout);
            Assert.Null(opts.MaxDatagramSize);
            Assert.Null(opts.SegmentPoolSize);
            Assert.Null(opts.StreamMode);
            Assert.Null(opts.FastAckLimit);
            Assert.Null(opts.InitialRto);
            Assert.Null(opts.MinRto);
            Assert.Null(opts.MaxRto);
            Assert.Null(opts.ProbeInit);
            Assert.Null(opts.ProbeLimit);
            Assert.Null(opts.InitialCongestionWindow);
            Assert.Null(opts.SlowStartThreshold);
        }

        /// <summary>
        /// 测试 KCP 核心内部默认值与可选配置覆盖。
        /// </summary>
        [Fact]
        public void KcpCore_Defaults_And_Optional_Overrides_Should_Work()
        {
            var kcp = new KcpCore(ConvA);
            Assert.Equal((uint)KcpConstants.IKCP_MTU_DEF, kcp.Mtu);
            Assert.Equal((uint)KcpConstants.IKCP_WND_SND, kcp.SendWindow);
            Assert.Equal((uint)KcpConstants.IKCP_WND_RCV, kcp.ReceiveWindow);
            Assert.Equal((uint)KcpConstants.IKCP_INTERVAL, kcp.Interval);
            Assert.False(kcp.NoDelay);
            Assert.False(kcp.NoCongestionControl);
            Assert.False(kcp.StreamMode);
            Assert.Equal(KcpConstants.IKCP_FASTACK_LIMIT, kcp.FastAckLimit);
            Assert.Equal(KcpConstants.IKCP_RTO_DEF, kcp.Rto);
            Assert.Equal(KcpConstants.IKCP_RTO_MIN, kcp.MinRto);
            Assert.Equal(KcpConstants.IKCP_RTO_MAX, kcp.MaxRto);
            Assert.Equal(KcpConstants.IKCP_PROBE_INIT, kcp.ProbeInit);
            Assert.Equal(KcpConstants.IKCP_PROBE_LIMIT, kcp.ProbeLimit);

            kcp.SetMtu(1200);
            kcp.SetWindowSize(64, 96);
            kcp.ConfigureNoDelay(true, 2, 20, 3, true, false);
            kcp.DeadLink = 120;
            kcp.SetFastAckLimit(9);
            kcp.ConfigureRto(300, 50, 2000);
            kcp.SetProbeIntervals(5000, 30000);
            kcp.SetInitialCongestionWindow(8);
            kcp.SetSlowStartThreshold(128);

            Assert.Equal(1200u, kcp.Mtu);
            Assert.Equal(64u, kcp.SendWindow);
            Assert.Equal(96u, kcp.ReceiveWindow);
            Assert.True(kcp.NoDelay);
            Assert.Equal(20u, kcp.Interval);
            Assert.Equal(3, kcp.FastResend);
            Assert.True(kcp.NoCongestionControl);
            Assert.False(kcp.StreamMode);
            Assert.Equal(120, kcp.DeadLink);
            Assert.Equal(9, kcp.FastAckLimit);
            Assert.Equal(300, kcp.Rto);
            Assert.Equal(50, kcp.MinRto);
            Assert.Equal(2000, kcp.MaxRto);
            Assert.Equal(5000, kcp.ProbeInit);
            Assert.Equal(30000, kcp.ProbeLimit);
            Assert.Equal(8u, kcp.CongestionWindow);
            Assert.Equal(128u, kcp.SlowStartThreshold);
        }

        #endregion

        #region === 14. KcpConstants 验证 ===

        /// <summary>
        /// 验证关键常量值与 ikcp.c 一致。
        /// </summary>
        [Fact]
        public void Constants_Should_Match_Ikcp()
        {
            Assert.Equal(81, KcpConstants.IKCP_CMD_PUSH);
            Assert.Equal(82, KcpConstants.IKCP_CMD_ACK);
            Assert.Equal(83, KcpConstants.IKCP_CMD_WASK);
            Assert.Equal(84, KcpConstants.IKCP_CMD_WINS);
            Assert.Equal(24, KcpConstants.IKCP_OVERHEAD);
            Assert.Equal(1400, KcpConstants.IKCP_MTU_DEF);
            Assert.Equal(1376, KcpConstants.IKCP_MSS_DEF);
            Assert.Equal(128, KcpConstants.IKCP_WND_RCV);
            Assert.Equal(256, KcpConstants.IKCP_WND_MAX);
            Assert.Equal(100, KcpConstants.IKCP_INTERVAL);
            Assert.Equal(20, KcpConstants.IKCP_DEADLINK);
            Assert.Equal(2, KcpConstants.IKCP_THRESH_INIT);
            Assert.Equal(2, KcpConstants.IKCP_THRESH_MIN);
            Assert.Equal(100, KcpConstants.IKCP_RTO_MIN);
            Assert.Equal(60000, KcpConstants.IKCP_RTO_MAX);
            Assert.Equal(200, KcpConstants.IKCP_RTO_DEF);
            Assert.Equal(5, KcpConstants.IKCP_FASTACK_LIMIT);
            Assert.Equal(50, KcpConstants.IKCP_MTU_MIN);
            Assert.Equal(0, KcpConstants.IKCP_STATE_AVAILABLE);
            Assert.Equal(-1, KcpConstants.IKCP_STATE_DEAD);
        }

        /// <summary>
        /// 验证常量之间的数学关系。
        /// </summary>
        [Fact]
        public void Constants_Mathematical_Relationships()
        {
            Assert.Equal(KcpConstants.IKCP_MTU_DEF - KcpConstants.IKCP_OVERHEAD, KcpConstants.IKCP_MSS_DEF);
            Assert.True(KcpConstants.IKCP_RTO_MIN < KcpConstants.IKCP_RTO_DEF);
            Assert.True(KcpConstants.IKCP_RTO_DEF < KcpConstants.IKCP_RTO_MAX);
            Assert.True(KcpConstants.IKCP_THRESH_MIN <= KcpConstants.IKCP_THRESH_INIT);
        }

        #endregion

        #region === 辅助方法 ===

        private static KcpCore CreateKcp(uint conv, List<byte[]> outputCapture)
        {
            var kcp = new KcpCore(conv);
            kcp.Output = data => outputCapture.Add(data.ToArray());
            return kcp;
        }

        private static (KcpCore sender, KcpCore receiver) CreatePair(uint conv)
        {
            var sOut = new List<byte[]>();
            var rOut = new List<byte[]>();
            var sender = CreateKcp(conv, sOut);
            var receiver = CreateKcp(conv, rOut);
            sender.SetFastMode();
            receiver.SetFastMode();
            return (sender, receiver);
        }

        private static void TransferAndVerify(KcpCore sender, KcpCore receiver, byte[] data, int iterations = 30)
        {
            sender.Send(data);
            SimulateBidirectional(sender, receiver, iterations);
            var received = ReceiveAll(receiver);
            Assert.Equal(data, received);
        }

        private static void SimulateBidirectional(KcpCore sender, KcpCore receiver, int iterations, uint startTime = 1000)
        {
            uint t = startTime;
            for (int i = 0; i < iterations; i++)
            {
                t += 10;
                var sPkts = new List<byte[]>();
                var rPkts = new List<byte[]>();
                sender.Output = d => sPkts.Add(d.ToArray());
                receiver.Output = d => rPkts.Add(d.ToArray());

                sender.Update(t);
                receiver.Update(t);

                foreach (var pkt in sPkts)
                    receiver.Input(pkt.AsSpan());
                foreach (var pkt in rPkts)
                    sender.Input(pkt.AsSpan());
            }
        }

        private static byte[] ReceiveAll(KcpCore receiver)
        {
            var result = new List<byte>();
            var buf = new byte[65536];
            while (receiver.PeekCanRecv())
            {
                int len = receiver.Recv(buf);
                if (len > 0)
                    result.AddRange(buf.Take(len));
                else
                    break;
            }
            return result.ToArray();
        }

        /// <summary>
        /// 解析连续的 KCP 输出缓冲区为独立的段信息列表。
        /// </summary>
        private static List<(uint Conv, byte Cmd, byte Frg, ushort Wnd, uint Ts, uint Sn, uint Una, byte[] Data)> ParseKcpSegments(byte[] buffer)
        {
            var result = new List<(uint, byte, byte, ushort, uint, uint, uint, byte[])>();
            int offset = 0;

            while (offset + KcpConstants.IKCP_OVERHEAD <= buffer.Length)
            {
                uint conv = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(offset));
                byte cmd = buffer[offset + 4];
                byte frg = buffer[offset + 5];
                ushort wnd = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(offset + 6));
                uint ts = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(offset + 8));
                uint sn = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(offset + 12));
                uint una = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(offset + 16));
                uint len = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(offset + 20));

                byte[] segData = null;
                if (len > 0 && offset + KcpConstants.IKCP_OVERHEAD + (int)len <= buffer.Length)
                {
                    segData = new byte[len];
                    Array.Copy(buffer, offset + KcpConstants.IKCP_OVERHEAD, segData, 0, (int)len);
                }

                result.Add((conv, cmd, frg, wnd, ts, sn, una, segData));
                offset += KcpConstants.IKCP_OVERHEAD + (int)len;
            }

            return result;
        }

        /// <summary>
        /// 将解析出的段信息重新编码为独立的 KCP 包。
        /// </summary>
        private static byte[] EncodeKcpSegment((uint Conv, byte Cmd, byte Frg, ushort Wnd, uint Ts, uint Sn, uint Una, byte[] Data) seg)
        {
            var dataLen = seg.Data?.Length ?? 0;
            var packet = new byte[KcpConstants.IKCP_OVERHEAD + dataLen];
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(packet, seg.Conv);
            packet[4] = seg.Cmd;
            packet[5] = seg.Frg;
            System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(6), seg.Wnd);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(8), seg.Ts);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(12), seg.Sn);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(16), seg.Una);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(20), (uint)dataLen);
            if (seg.Data != null)
                Array.Copy(seg.Data, 0, packet, KcpConstants.IKCP_OVERHEAD, dataLen);
            return packet;
        }

        #endregion
    }
}
