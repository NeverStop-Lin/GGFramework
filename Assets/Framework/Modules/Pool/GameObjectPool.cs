using System.Collections.Generic;
using UnityEngine;

namespace Framework.Modules.Pool
{
    /// <summary>
    /// GameObject对象池
    /// 用于管理Unity游戏对象的复用
    /// </summary>
    public class GameObjectPool
    {
        #region 字段

        /// <summary>预制体</summary>
        private readonly GameObject _prefab;

        /// <summary>对象池</summary>
        private readonly Stack<GameObject> _pool;

        /// <summary>父节点</summary>
        private readonly Transform _parent;

        /// <summary>池名称</summary>
        private readonly string _poolName;

        /// <summary>最小池大小</summary>
        private readonly int _minSize;

        /// <summary>最大池大小</summary>
        private readonly int _maxSize;

        /// <summary>当前活跃对象数量</summary>
        private int _activeCount;

        /// <summary>自动回收到父节点</summary>
        private readonly bool _autoReturnToParent;

        #endregion

        #region 属性

        /// <summary>池中可用对象数量</summary>
        public int AvailableCount => _pool.Count;

        /// <summary>活跃对象数量</summary>
        public int ActiveCount => _activeCount;

        /// <summary>总对象数量</summary>
        public int TotalCount => AvailableCount + ActiveCount;

        /// <summary>池名称</summary>
        public string PoolName => _poolName;

        #endregion

        #region 构造函数

        /// <summary>
        /// 创建GameObject对象池
        /// </summary>
        /// <param name="prefab">预制体</param>
        /// <param name="parent">父节点</param>
        /// <param name="minSize">最小池大小</param>
        /// <param name="maxSize">最大池大小</param>
        /// <param name="autoReturnToParent">回收时是否自动移回父节点</param>
        public GameObjectPool(
            GameObject prefab,
            Transform parent = null,
            int minSize = 0,
            int maxSize = 100,
            bool autoReturnToParent = true)
        {
            _prefab = prefab;
            _parent = parent;
            _poolName = prefab.name;
            _minSize = minSize;
            _maxSize = maxSize;
            _autoReturnToParent = autoReturnToParent;
            _pool = new Stack<GameObject>();
            _activeCount = 0;

            // 如果没有指定父节点，创建一个
            if (_parent == null)
            {
                var poolRoot = new GameObject($"Pool_{_poolName}");
                _parent = poolRoot.transform;
                Object.DontDestroyOnLoad(poolRoot);
            }

            // 预创建最小数量的对象
            Prewarm(_minSize);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 从池中获取对象
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="rotation">旋转</param>
        /// <param name="parent">父节点</param>
        /// <returns>游戏对象</returns>
        public GameObject Spawn(Vector3? position = null, Quaternion? rotation = null, Transform parent = null)
        {
            GameObject obj;

            if (_pool.Count > 0)
            {
                obj = _pool.Pop();
            }
            else
            {
                obj = Object.Instantiate(_prefab);
                obj.name = _poolName;
            }

            // 设置位置和旋转
            if (position.HasValue)
                obj.transform.position = position.Value;
            if (rotation.HasValue)
                obj.transform.rotation = rotation.Value;
            if (parent != null)
                obj.transform.SetParent(parent);

            obj.SetActive(true);
            _activeCount++;

            return obj;
        }

        /// <summary>
        /// 获取带组件的对象
        /// </summary>
        public T Spawn<T>(Vector3? position = null, Quaternion? rotation = null, Transform parent = null) where T : Component
        {
            var obj = Spawn(position, rotation, parent);
            return obj.GetComponent<T>();
        }

        /// <summary>
        /// 将对象归还到池中
        /// </summary>
        public void Despawn(GameObject obj)
        {
            if (obj == null)
                return;

            obj.SetActive(false);

            // 移回池父节点
            if (_autoReturnToParent)
            {
                obj.transform.SetParent(_parent);
            }

            // 如果超过最大容量，直接销毁
            if (_pool.Count >= _maxSize)
            {
                Object.Destroy(obj);
            }
            else
            {
                _pool.Push(obj);
            }

            _activeCount--;
        }

        /// <summary>
        /// 将组件对象归还到池中
        /// </summary>
        public void Despawn<T>(T component) where T : Component
        {
            if (component != null)
            {
                Despawn(component.gameObject);
            }
        }

        /// <summary>
        /// 清空对象池
        /// </summary>
        public void Clear()
        {
            while (_pool.Count > 0)
            {
                var obj = _pool.Pop();
                Object.Destroy(obj);
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

                var obj = Object.Instantiate(_prefab, _parent);
                obj.name = _poolName;
                obj.SetActive(false);
                _pool.Push(obj);
            }
        }

        /// <summary>
        /// 收缩池（移除多余的未使用对象）
        /// </summary>
        public void Shrink(int targetSize)
        {
            while (_pool.Count > targetSize)
            {
                var obj = _pool.Pop();
                Object.Destroy(obj);
            }
        }

        #endregion
    }
}

