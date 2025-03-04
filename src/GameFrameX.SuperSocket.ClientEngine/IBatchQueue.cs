using System;
using System.Collections.Generic;

namespace GameFrameX.SuperSocket.ClientEngine
{
    /// <summary>
    /// 批量队列接口，用于处理批量数据的入队和出队操作
    /// </summary>
    /// <typeparam name="T">队列中元素的类型</typeparam>
    public interface IBatchQueue<T>
    {
        /// <summary>
        /// 将单个元素添加到队列中
        /// </summary>
        /// <param name="item">要入队的元素</param>
        /// <returns>入队操作是否成功</returns>
        bool Enqueue(T item);

        /// <summary>
        /// 将多个元素添加到队列中
        /// </summary>
        /// <param name="items">要入队的元素列表</param>
        /// <returns>入队操作是否成功</returns>
        bool Enqueue(IList<T> items);

        /// <summary>
        /// 尝试从队列中取出元素
        /// </summary>
        /// <param name="outputItems">用于存储出队元素的列表</param>
        /// <returns>出队操作是否成功</returns>
        bool TryDequeue(IList<T> outputItems);

        /// <summary>
        /// 获取队列是否为空
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// 获取队列中的元素数量
        /// </summary>
        int Count { get; }
    }
}