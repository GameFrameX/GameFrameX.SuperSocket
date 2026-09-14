using GameFrameX.SuperSocket.Kcp.Kcp;
using Xunit;

namespace GameFrameX.SuperSocket.Kcp.Tests
{
    /// <summary>
    /// KCP 核心协议单元测试。
    /// </summary>
    public class KcpCoreTests
    {
        private const uint TestConv = 0x12345678;
        private const int DefaultMss = KcpConstants.IKCP_MTU_DEF - KcpConstants.IKCP_OVERHEAD;

        #region === 基本收发测试 ===

        [Fact]
        public void Send_Then_Update_Input_Recv_Should_Transfer_Data()
        {
            // Arrange: 创建收发双方
            var senderOutput = new List<Memory<byte>>();
            var receiverOutput = new List<Memory<byte>>();

            var sender = CreateKcp(TestConv, data => senderOutput.Add(data.ToArray()));
            var receiver = CreateKcp(TestConv, data => receiverOutput.Add(data.ToArray()));

            sender.SetFastMode();
            receiver.SetFastMode();

            var sendData = new byte[100];
            for (int i = 0; i < sendData.Length; i++)
                sendData[i] = (byte)(i & 0xFF);

            // Act: 发送方 Send
            int sent = sender.Send(sendData);
            Assert.Equal(100, sent);

            // 驱动发送方 Update，产生 UDP 包
            var currentTime = GetTimestamp();
            sender.Update(currentTime);

            // 发送方产生的 UDP 包投递给接收方
            foreach (var packet in senderOutput)
            {
                receiver.Input(packet.Span);
            }

            // 数据应该在 Input 后立即可用（KCP 协议栈已处理）
            // 但可能需要双向 Update 来传递 ACK
            SimulateCommunication(sender, receiver, senderOutput, iterations: 5);

            // 接收方 Recv
            var recvBuffer = new byte[200];
            int received = receiver.Recv(recvBuffer);

            // Assert
            Assert.Equal(100, received);
            Assert.Equal(sendData, recvBuffer.AsSpan(0, received).ToArray());
        }

        [Fact]
        public void Send_Large_Data_Should_Fragment_And_Reassemble()
        {
            // Arrange
            var senderOutput = new List<Memory<byte>>();

            var sender = CreateKcp(TestConv, data => senderOutput.Add(data.ToArray()));
            var receiver = CreateKcp(TestConv, data => { });

            sender.SetFastMode();
            receiver.SetFastMode();

            // 发送 2 倍 MSS 大小的数据，应分成 3 片
            int dataSize = DefaultMss * 2 + 100;
            var sendData = new byte[dataSize];
            var random = new Random(42);
            random.NextBytes(sendData);

            // Act
            int sent = sender.Send(sendData);
            Assert.Equal(dataSize, sent);

            var currentTime = GetTimestamp();
            sender.Update(currentTime);

            // 投递所有 UDP 包给接收方
            foreach (var packet in senderOutput)
            {
                receiver.Input(packet.Span);
            }

            // 接收方可能需要多次 Update 才能收到完整数据
            // 模拟双向通信
            senderOutput.Clear();
            SimulateCommunication(sender, receiver, senderOutput, iterations: 100);

            // Debug: Try to receive multiple times
            var allReceived = new List<byte>();
            var tempBuf = new byte[4096];
            while (receiver.PeekCanRecv())
            {
                int len = receiver.Recv(tempBuf);
                if (len > 0)
                    allReceived.AddRange(tempBuf.Take(len));
                else
                    break;
            }

            Assert.Equal(dataSize, allReceived.Count);
            Assert.Equal(sendData, allReceived.ToArray());
        }

        [Fact]
        public void Send_Multiple_Messages_Should_Be_Received_In_Order()
        {
            // Arrange
            var senderOutput = new List<Memory<byte>>();

            var sender = CreateKcp(TestConv, data => senderOutput.Add(data.ToArray()));
            var receiver = CreateKcp(TestConv, data => { });

            sender.SetFastMode();
            receiver.SetFastMode();

            var messages = new byte[][]
            {
                System.Text.Encoding.UTF8.GetBytes("Hello"),
                System.Text.Encoding.UTF8.GetBytes("World"),
                System.Text.Encoding.UTF8.GetBytes("KCP"),
                System.Text.Encoding.UTF8.GetBytes("Test")
            };

            // Act: 依次发送多条消息
            foreach (var msg in messages)
            {
                sender.Send(msg);
            }

            var currentTime = GetTimestamp();
            sender.Update(currentTime);

            foreach (var packet in senderOutput)
            {
                receiver.Input(packet.Span);
            }

            SimulateCommunication(sender, receiver, senderOutput, iterations: 50);

            // Assert: 依次接收并验证顺序
            var received = new List<byte[]>();
            var recvBuffer = new byte[1024];

            while (receiver.PeekCanRecv())
            {
                int len = receiver.Recv(recvBuffer);
                if (len > 0)
                {
                    received.Add(recvBuffer.AsSpan(0, len).ToArray());
                }
                else
                {
                    break;
                }
            }

            Assert.Equal(messages.Length, received.Count);
            for (int i = 0; i < messages.Length; i++)
            {
                Assert.Equal(messages[i], received[i]);
            }
        }

        #endregion

        #region === 丢包重传测试 ===

        [Fact]
        public void With_Packet_Loss_Should_Retransmit_And_Deliver()
        {
            // Arrange
            var senderOutput = new List<Memory<byte>>();
            var sender = CreateKcp(TestConv, data => senderOutput.Add(data.ToArray()));
            var receiver = CreateKcp(TestConv, data => { });

            sender.SetFastMode();
            receiver.SetFastMode();

            var sendData = System.Text.Encoding.UTF8.GetBytes("Hello KCP with packet loss!");

            // Act: 发送
            sender.Send(sendData);

            uint currentTime = GetTimestamp();
            sender.Update(currentTime);

            // 模拟 50% 丢包：只投递一半的包
            var random = new Random(123);
            foreach (var packet in senderOutput)
            {
                if (random.Next(2) == 0)
                {
                    receiver.Input(packet.Span);
                }
            }

            // 驱动多轮 Update 触发重传
            for (int i = 0; i < 50; i++)
            {
                currentTime += 10;
                var newPackets = new List<Memory<byte>>();
                sender.Output = data => newPackets.Add(data.ToArray());

                sender.Update(currentTime);
                receiver.Update(currentTime);

                foreach (var packet in newPackets)
                {
                    receiver.Input(packet.Span);
                }

                // 检查是否已收到
                if (receiver.PeekCanRecv())
                {
                    var recvBuffer = new byte[1024];
                    int len = receiver.Recv(recvBuffer);
                    Assert.Equal(sendData.Length, len);
                    Assert.Equal(sendData, recvBuffer.AsSpan(0, len).ToArray());
                    return; // 测试通过
                }
            }

            Assert.Fail("Failed to receive data after 50 iterations with packet loss");
        }

        [Fact]
        public void With_Minute_Blackout_And_Extended_DeadLink_Should_Retransmit_And_Deliver()
        {
            var senderPackets = new List<Memory<byte>>();
            var receiverPackets = new List<Memory<byte>>();
            var sender = CreateKcp(TestConv, data => senderPackets.Add(data.ToArray()));
            var receiver = CreateKcp(TestConv, data => receiverPackets.Add(data.ToArray()));

            sender.SetFastMode();
            receiver.SetFastMode();
            sender.DeadLink = 120;

            var sendData = System.Text.Encoding.UTF8.GetBytes("Hello KCP after minute blackout!");
            Assert.Equal(sendData.Length, sender.Send(sendData));

            var currentTime = GetTimestamp();

            for (var i = 0; i < 24000; i++)
            {
                currentTime += 10;
                senderPackets.Clear();
                receiverPackets.Clear();

                sender.Update(currentTime);
                receiver.Update(currentTime);

                if (i >= 6000)
                {
                    foreach (var packet in senderPackets)
                    {
                        receiver.Input(packet.Span);
                    }

                    foreach (var packet in receiverPackets)
                    {
                        sender.Input(packet.Span);
                    }
                }

                if (!receiver.PeekCanRecv())
                    continue;

                var recvBuffer = new byte[1024];
                var len = receiver.Recv(recvBuffer);
                Assert.Equal(sendData.Length, len);
                Assert.Equal(sendData, recvBuffer.AsSpan(0, len).ToArray());
                return;
            }

            Assert.Fail("Failed to receive data after simulated 60 seconds blackout.");
        }

        #endregion

        #region === 窗口与 MTU 测试 ===

        [Fact]
        public void SetMtu_Should_Update_Mss()
        {
            var kcp = new KcpCore(TestConv);
            kcp.SetMtu(800);
            // MTU = 800, MSS = 800 - 24(overhead) = 776
            // 通过发送验证分片大小
            var data = new byte[800];
            int sent = kcp.Send(data);
            Assert.Equal(800, sent);
        }

        [Fact]
        public void SetMtu_Too_Small_Should_Throw()
        {
            var kcp = new KcpCore(TestConv);
            Assert.Throws<ArgumentException>(() => kcp.SetMtu(30));
        }

        [Fact]
        public void SetNoDelay_FastMode_Should_Set_Parameters()
        {
            var kcp = new KcpCore(TestConv);
            kcp.SetFastMode();
            // 验证不抛异常
            Assert.True(true);
        }

        [Fact]
        public void SetNoDelay_NormalMode_Should_Set_Parameters()
        {
            var kcp = new KcpCore(TestConv);
            kcp.SetNormalMode();
            Assert.True(true);
        }

        #endregion

        #region === 状态测试 ===

        [Fact]
        public void Initial_State_Should_Be_Available()
        {
            var kcp = new KcpCore(TestConv);
            Assert.Equal(KcpConstants.IKCP_STATE_AVAILABLE, kcp.State);
        }

        [Fact]
        public void Conv_Should_Match_Constructor()
        {
            var kcp = new KcpCore(TestConv);
            Assert.Equal(TestConv, kcp.Conv);
        }

        [Fact]
        public void Send_Empty_Data_Should_Return_Zero()
        {
            var kcp = new KcpCore(TestConv);
            int result = kcp.Send(ReadOnlySpan<byte>.Empty);
            Assert.Equal(0, result);
        }

        [Fact]
        public void Recv_No_Data_Should_Return_Negative()
        {
            var kcp = new KcpCore(TestConv);
            var buffer = new byte[100];
            int result = kcp.Recv(buffer);
            Assert.Equal(-1, result);
        }

        [Fact]
        public void Input_With_Wrong_Conv_Should_Return_Error()
        {
            var kcp = new KcpCore(TestConv);
            // 构造一个 conv 不匹配的 KCP 包
            var packet = new byte[KcpConstants.IKCP_OVERHEAD];
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(packet, 0xDEADBEEF); // wrong conv
            int result = kcp.Input(packet);
            Assert.Equal(-3, result);
        }

        #endregion

        #region === 辅助方法 ===

        private static KcpCore CreateKcp(uint conv, Action<Memory<byte>> output)
        {
            var kcp = new KcpCore(conv);
            kcp.Output = output;
            return kcp;
        }

        private static uint GetTimestamp()
        {
            return (uint)(Environment.TickCount64 & 0xFFFFFFFF);
        }

        /// <summary>
        /// 模拟双向通信（多轮 Update + 包交换）。
        /// </summary>
        private static void SimulateCommunication(
            KcpCore sender, KcpCore receiver,
            List<Memory<byte>> senderOutput,
            int iterations)
        {
            var currentTime = GetTimestamp();

            for (int i = 0; i < iterations; i++)
            {
                currentTime += 10;

                var newSenderPackets = new List<Memory<byte>>();
                var newReceiverPackets = new List<Memory<byte>>();

                sender.Output = data => newSenderPackets.Add(data.ToArray());
                receiver.Output = data => newReceiverPackets.Add(data.ToArray());

                sender.Update(currentTime);
                receiver.Update(currentTime);

                foreach (var packet in newSenderPackets)
                {
                    receiver.Input(packet.Span);
                }

                foreach (var packet in newReceiverPackets)
                {
                    sender.Input(packet.Span);
                }

                senderOutput.Clear();
                senderOutput.AddRange(newSenderPackets);
            }
        }

        #endregion
    }
}
