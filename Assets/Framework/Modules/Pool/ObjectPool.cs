using System;
using System.Collections.Generic;

namespace Framework.Modules.Pool
{
    /// <summary>
    /// 泛型对象池
    /// 用于管理普通C#对象的复用
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    public class ObjectPool<T> where T : class, new()
    {
        #region 字段

        /// <summary>对象池</summary>
        private readonly Stack<T> _pool;

        /// <summary>对象工厂方法</summary>
        private readonly Func<T> _createFunc;

        /// <summary>对象重置方法</summary>
        private readonly Action<T> _onGet;

        /// <summary>对象归还方法</summary>
        private readonly Action<T> _onReturn;

        /// <summary>对象销毁方法</summary>
        private readonly Action<T> _onDestroy;

        /// <summary>最小池大小</summary>
        private readonly int _minSize;

        /// <summary>最大池大小</summary>
        private readonly int _maxSize;

        /// <summary>当前活跃对象数量</summary>
        private int _activeCount;

        #endregion

        #region 属性

        /// <summary>池中可用对象数量</summary>
        public int AvailableCount => _pool.Count;

        /// <summary>活跃对象数量</summary>
        public int ActiveCount => _activeCount;

        /// <summary>总对象数量</summary>
        public int TotalCount => AvailableCount + ActiveCount;

        #endregion

        #region 构造函数

        /// <summary>
        /// 创建对象池
        /// </summary>
        /// <param name="createFunc">对象创建方法（null则使用new T()）</param>
        /// <param name="onGet">获取对象时的回调</param>
        /// <param name="onReturn">归还对象时的回调</param>
        /// <param name="onDestroy">销毁对象时的回调</param>
        /// <param name="minSize">最小池大小</param>
        /// <param name="maxSize">最大池大小</param>
        public ObjectPool(
            Func<T> createFunc = null,
            Action<T> onGet = null,
            Action<T> onReturn = null,
            Action<T> onDestroy = null,
            int minSize = 0,
            int maxSize = 100)
        {
            _pool = new Stack<T>();
            _createFunc = createFunc ?? (() => new T());
            _onGet = onGet;
            _onReturn = onReturn;
            _onDestroy = onDestroy;
            _minSize = minSize;
            _maxSize = maxSize;
            _activeCount = 0;

            // 预创建最小数量的对象
            for (int i = 0; i < _minSize; i++)
            {
                var obj = _createFunc();
                _pool.Push(obj);
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 从池中获取对象
        /// </summary>
        public T Spawn()
        {
            T obj;

            if (_pool.Count > 0)
            {
                obj = _pool.Pop();
            }
            else
            {
                obj = _createFunc();
            }

            _activeCount++;
            _onGet?.Invoke(obj);
            return obj;
        }

        /// <summary>
        /// 将对象归还到池中
        /// </summary>
        public void Despawn(T obj)
        {
            if (obj == null)
                return;

            _onReturn?.Invoke(obj);

            // 如果超过最大容量，直接销毁
            if (_pool.Count >= _maxSize)
            {
                _onDestroy?.Invoke(obj);
            }
            else
            {
                _pool.Push(obj);
            }

            _activeCount--;
        }

        /// <summary>
        /// 清空对象池
        /// </summary>
        public void Clear()
        {
            while (_pool.Count > 0)
            {
                var obj = _pool.Pop();
                _onDestroy?.Invoke(obj);
            }

            _activeCount = 0;
        }

        /// <summary>
        /// 预热对象池（预创建指定数量的对象）
        /// </summary>
        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (_pool.Count >= _maxSize)
                    break;

                var obj = _createFunc();
                _pool.Push(obj);
            }
        }

        #endregion
    }
}

