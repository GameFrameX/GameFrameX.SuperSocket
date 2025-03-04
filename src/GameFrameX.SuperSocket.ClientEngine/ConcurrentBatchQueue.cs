using System;
using System.Collections.Generic;
using System.Threading;

namespace GameFrameX.SuperSocket.ClientEngine
{
    /// <summary>
    /// 并发批处理队列类
    /// </summary>
    /// <typeparam name="T">队列中元素的类型</typeparam>
    public class ConcurrentBatchQueue<T> : IBatchQueue<T>
    {
        /// <summary>
        /// 使用默认容量16初始化并发批处理队列的新实例
        /// </summary>
        public ConcurrentBatchQueue() : this(16)
        {
        }

        /// <summary>
        /// 使用指定容量初始化并发批处理队列的新实例
        /// </summary>
        /// <param name="capacity">队列的初始容量</param>
        public ConcurrentBatchQueue(int capacity) : this(new T[capacity])
        {
        }

        /// <summary>
        /// 使用指定容量和空值验证器初始化并发批处理队列的新实例
        /// </summary>
        /// <param name="capacity">队列的初始容量</param>
        /// <param name="nullValidator">用于验证元素是否为空的函数</param>
        public ConcurrentBatchQueue(int capacity, Func<T, bool> nullValidator) : this(new T[capacity], nullValidator)
        {
        }

        /// <summary>
        /// 使用指定数组初始化并发批处理队列的新实例
        /// </summary>
        /// <param name="array">用于存储队列元素的数组</param>
        public ConcurrentBatchQueue(T[] array) : this(array, (T t) => t == null)
        {
        }

        /// <summary>
        /// 使用指定数组和空值验证器初始化并发批处理队列的新实例
        /// </summary>
        /// <param name="array">用于存储队列元素的数组</param>
        /// <param name="nullValidator">用于验证元素是否为空的函数</param>
        public ConcurrentBatchQueue(T[] array, Func<T, bool> nullValidator)
        {
            this.m_Entity = new ConcurrentBatchQueue<T>.Entity
            {
                Array = array
            };
            this.m_BackEntity = new ConcurrentBatchQueue<T>.Entity();
            this.m_BackEntity.Array = new T[array.Length];
            this.m_NullValidator = nullValidator;
        }

        /// <summary>
        /// 将元素添加到队列中
        /// </summary>
        /// <param name="item">要添加的元素</param>
        /// <returns>如果添加成功返回true，队列已满返回false</returns>
        public bool Enqueue(T item)
        {
            bool flag;
            while (!this.TryEnqueue(item, out flag) && !flag)
            {
            }

            return !flag;
        }

        /// <summary>
        /// 尝试将元素添加到队列中
        /// </summary>
        /// <param name="item">要添加的元素</param>
        /// <param name="full">输出参数，指示队列是否已满</param>
        /// <returns>如果添加成功返回true，否则返回false</returns>
        private bool TryEnqueue(T item, out bool full)
        {
            full = false;
            ConcurrentBatchQueue<T>.Entity entity = this.m_Entity as ConcurrentBatchQueue<T>.Entity;
            T[] array = entity.Array;
            int count = entity.Count;
            if (count >= array.Length)
            {
                full = true;
                return false;
            }

            if (entity != this.m_Entity)
            {
                return false;
            }

            if (Interlocked.CompareExchange(ref entity.Count, count + 1, count) != count)
            {
                return false;
            }

            array[count] = item;
            return true;
        }

        /// <summary>
        /// 将元素集合添加到队列中
        /// </summary>
        /// <param name="items">要添加的元素集合</param>
        /// <returns>如果添加成功返回true，队列已满返回false</returns>
        public bool Enqueue(IList<T> items)
        {
            bool flag;
            while (!this.TryEnqueue(items, out flag) && !flag)
            {
            }

            return !flag;
        }

        /// <summary>
        /// 尝试将元素集合添加到队列中
        /// </summary>
        /// <param name="items">要添加的元素集合</param>
        /// <param name="full">输出参数，指示队列是否已满</param>
        /// <returns>如果添加成功返回true，否则返回false</returns>
        private bool TryEnqueue(IList<T> items, out bool full)
        {
            full = false;
            ConcurrentBatchQueue<T>.Entity entity = this.m_Entity as ConcurrentBatchQueue<T>.Entity;
            T[] array = entity.Array;
            int count = entity.Count;
            int count2 = items.Count;
            int num = count + count2;
            if (num > array.Length)
            {
                full = true;
                return false;
            }

            if (entity != this.m_Entity)
            {
                return false;
            }

            if (Interlocked.CompareExchange(ref entity.Count, num, count) != count)
            {
                return false;
            }

            foreach (T t in items)
            {
                array[count++] = t;
            }

            return true;
        }

        /// <summary>
        /// 尝试从队列中移除并返回元素集合
        /// </summary>
        /// <param name="outputItems">用于存储移除元素的集合</param>
        /// <returns>如果成功移除元素返回true，队列为空返回false</returns>
        public bool TryDequeue(IList<T> outputItems)
        {
            ConcurrentBatchQueue<T>.Entity entity = this.m_Entity as ConcurrentBatchQueue<T>.Entity;
            if (entity.Count <= 0)
            {
                return false;
            }

            if (Interlocked.CompareExchange(ref this.m_Entity, this.m_BackEntity, entity) != entity)
            {
                return false;
            }

            SpinWait spinWait = default(SpinWait);
            spinWait.SpinOnce();
            int count = entity.Count;
            T[] array = entity.Array;
            int num = 0;
            for (;;)
            {
                T t = array[num];
                while (this.m_NullValidator(t))
                {
                    spinWait.SpinOnce();
                    t = array[num];
                }

                outputItems.Add(t);
                array[num] = ConcurrentBatchQueue<T>.m_Null;
                if (entity.Count <= num + 1)
                {
                    break;
                }

                num++;
            }

            entity.Count = 0;
            this.m_BackEntity = entity;
            return true;
        }

        /// <summary>
        /// 获取队列是否为空
        /// </summary>
        public bool IsEmpty
        {
            get { return this.Count <= 0; }
        }

        /// <summary>
        /// 获取队列中的元素数量
        /// </summary>
        public int Count
        {
            get { return ((ConcurrentBatchQueue<T>.Entity)this.m_Entity).Count; }
        }

        /// <summary>
        /// 当前实体对象
        /// </summary>
        private object m_Entity;

        /// <summary>
        /// 备用实体对象
        /// </summary>
        private ConcurrentBatchQueue<T>.Entity m_BackEntity;

        /// <summary>
        /// 空值
        /// </summary>
        private static readonly T m_Null = default;

        /// <summary>
        /// 空值验证器
        /// </summary>
        private Func<T, bool> m_NullValidator;

        /// <summary>
        /// 实体类，用于存储队列数据
        /// </summary>
        private class Entity
        {
            /// <summary>
            /// 获取或设置数据数组
            /// </summary>
            public T[] Array { get; set; }

            /// <summary>
            /// 元素数量
            /// </summary>
            public int Count;
        }
    }
}