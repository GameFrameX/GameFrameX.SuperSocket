using System;
using System.Buffers;
using System.Text;
using GameFrameX.SuperSocket.ProtoBase;

namespace GameFrameX.SuperSocket.WebSocket
{
    public static partial class ExtensionMethods
    {
        private readonly static char[] m_CrCf = new char[] { '\r', '\n' };

        /// <summary>
        /// 使用回车换行符作为后缀追加格式化内容
        /// </summary>
        /// <param name="builder">StringBuilder构建器</param>
        /// <param name="format">格式化字符串</param>
        /// <param name="arg">格式化参数</param>
        public static void AppendFormatWithCrCf(this StringBuilder builder, string format, object arg)
        {
            builder.AppendFormat(format, arg);
            builder.Append(m_CrCf);
        }

        /// <summary>
        /// 使用回车换行符作为后缀追加格式化内容
        /// </summary>
        /// <param name="builder">StringBuilder构建器</param>
        /// <param name="format">格式化字符串</param>
        /// <param name="args">格式化参数数组</param>
        public static void AppendFormatWithCrCf(this StringBuilder builder, string format, params object[] args)
        {
            builder.AppendFormat(format, args);
            builder.Append(m_CrCf);
        }

        /// <summary>
        /// 使用回车换行符作为后缀追加内容
        /// </summary>
        /// <param name="builder">StringBuilder构建器</param>
        /// <param name="content">要追加的内容</param>
        public static void AppendWithCrCf(this StringBuilder builder, string content)
        {
            builder.Append(content);
            builder.Append(m_CrCf);
        }

        /// <summary>
        /// 追加回车换行符
        /// </summary>
        /// <param name="builder">StringBuilder构建器</param>
        public static void AppendWithCrCf(this StringBuilder builder)
        {
            builder.Append(m_CrCf);
        }

        /// <summary>
        /// 复制字节序列
        /// </summary>
        /// <param name="seq">要复制的字节序列</param>
        /// <returns>复制后的新字节序列</returns>
        internal static ReadOnlySequence<byte> CopySequence(ref this ReadOnlySequence<byte> seq)
        {
            SequenceSegment head = null;
            SequenceSegment tail = null;

            foreach (var segment in seq)
            {
                var newSegment = SequenceSegment.CopyFrom(segment);

                if (head == null)
                    tail = head = newSegment;
                else
                    tail = tail.SetNext(newSegment);
            }

            return new ReadOnlySequence<byte>(head, 0, tail, tail.Memory.Length);
        }

        /// <summary>
        /// 解构字节序列，获取头尾节点
        /// </summary>
        /// <param name="first">要解构的字节序列</param>
        /// <returns>包含头尾节点的元组</returns>
        internal static (SequenceSegment, SequenceSegment) DestructSequence(ref this ReadOnlySequence<byte> first)
        {
            SequenceSegment head = first.Start.GetObject() as SequenceSegment;
            SequenceSegment tail = first.End.GetObject() as SequenceSegment;

            if (head == null)
            {
                foreach (var segment in first)
                {
                    if (head == null)
                        tail = head = new SequenceSegment(segment);
                    else
                        tail = tail.SetNext(new SequenceSegment(segment));
                }
            }

            return (head, tail);
        }

        /// <summary>
        /// 连接两个字节序列
        /// </summary>
        /// <param name="first">第一个字节序列</param>
        /// <param name="second">第二个字节序列</param>
        /// <returns>连接后的新字节序列</returns>
        internal static ReadOnlySequence<byte> ConcatSequence(ref this ReadOnlySequence<byte> first, ref ReadOnlySequence<byte> second)
        {
            var (head, tail) = first.DestructSequence();

            if (!second.IsEmpty)
            {
                foreach (var segment in second)
                {
                    tail = tail.SetNext(new SequenceSegment(segment));
                }
            }

            return new ReadOnlySequence<byte>(head, 0, tail, tail.Memory.Length);
        }

        /// <summary>
        /// 将单个序列段连接到字节序列末尾
        /// </summary>
        /// <param name="first">原字节序列</param>
        /// <param name="segment">要连接的序列段</param>
        /// <returns>连接后的新字节序列</returns>
        internal static ReadOnlySequence<byte> ConcatSequence(ref this ReadOnlySequence<byte> first, SequenceSegment segment)
        {
            var (head, tail) = first.DestructSequence();
            tail = tail.SetNext(segment);
            return new ReadOnlySequence<byte>(head, 0, tail, tail.Memory.Length);
        }
    }
}