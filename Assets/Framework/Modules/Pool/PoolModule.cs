using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Framework.Modules.Pool
{
    /// <summary>
    /// 对象池模块
    /// 统一管理GameObject对象池和普通对象池
    /// </summary>
    public class PoolModule
    {
        #region 字段

        /// <summary>GameObject对象池字典</summary>
        private readonly Dictionary<string, GameObjectPool> _gameObjectPools;

        /// <summary>普通对象池字典（类型名 -> 池对象）</summary>
        private readonly Dictionary<string, object> _objectPools;

        #endregion

        #region 属性

        /// <summary>
        /// GameObject池数量
        /// </summary>
        public int GameObjectPoolCount => _gameObjectPools.Count;

        /// <summary>
        /// 普通对象池数量
        /// </summary>
        public int ObjectPoolCount => _objectPools.Count;

        #endregion

        #region 构造函数

        [Inject]
        public PoolModule()
        {
            _gameObjectPools = new Dictionary<string, GameObjectPool>();
            _objectPools = new Dictionary<string, object>();
            Debug.Log("[PoolModule] 对象池模块初始化完成");
        }

        #endregion

        #region GameObject对象池

        /// <summary>
        /// 创建GameObject对象池
        /// </summary>
        /// <param name="prefab">预制体</param>
        /// <param name="poolName">池名称（null则使用预制体名称）</param>
        /// <param name="parent">父节点</param>
        /// <param name="minSize">最小池大小</param>
        /// <param name="maxSize">最大池大小</param>
        /// <returns>对象池</returns>
        public GameObjectPool CreateGameObjectPool(
            GameObject prefab,
            string poolName = null,
            Transform parent = null,
            int minSize = 0,
            int maxSize = 100)
        {
            if (prefab == null)
            {
                Debug.LogError("[PoolModule] 预制体为空");
                return null;
            }

            poolName = poolName ?? prefab.name;

            if (_gameObjectPools.ContainsKey(poolName))
            {
                Debug.LogWarning($"[PoolModule] 对象池已存在: {poolName}");
                return _gameObjectPools[poolName];
            }

            var pool = new GameObjectPool(prefab, parent, minSize, maxSize);
            _gameObjectPools[poolName] = pool;
            Debug.Log($"[PoolModule] 创建GameObject对象池: {poolName}");
            return pool;
        }

        /// <summary>
        /// 获取GameObject对象池
        /// </summary>
        public GameObjectPool GetGameObjectPool(string poolName)
        {
            if (_gameObjectPools.TryGetValue(poolName, out var pool))
            {
                return pool;
            }
            Debug.LogWarning($"[PoolModule] 对象池不存在: {poolName}");
            return null;
        }

        /// <summary>
        /// 从池中获取GameObject（如果池不存在会自动创建）
        /// </summary>
        /// <param name="prefab">预制体</param>
        /// <param name="position">位置</param>
        /// <param name="rotation">旋转</param>
        /// <param name="parent">父节点</param>
        /// <returns>游戏对象</returns>
        public GameObject Spawn(
            GameObject prefab,
            Vector3? position = null,
            Quaternion? rotation = null,
            Transform parent = null)
        {
            if (prefab == null)
            {
                Debug.LogError("[PoolModule] 预制体为空");
                return null;
            }

            var poolName = prefab.name;
            if (!_gameObjectPools.ContainsKey(poolName))
            {
                CreateGameObjectPool(prefab, poolName);
            }

            return _gameObjectPools[poolName].Spawn(position, rotation, parent);
        }

        /// <summary>
        /// 从池中获取带组件的GameObject
        /// </summary>
        public T Spawn<T>(
            GameObject prefab,
            Vector3? position = null,
            Quaternion? rotation = null,
            Transform parent = null) where T : Component
        {
            var obj = Spawn(prefab, position, rotation, parent);
            return obj?.GetComponent<T>();
        }

        /// <summary>
        /// 将GameObject归还到池中
        /// </summary>
        public void Despawn(GameObject obj)
        {
            if (obj == null)
                return;

            var poolName = obj.name;
            if (_gameObjectPools.TryGetValue(poolName, out var pool))
            {
                pool.Despawn(obj);
            }
            else
            {
                Debug.LogWarning($"[PoolModule] 未找到对应的对象池，直接销毁: {poolName}");
                UnityEngine.Object.Destroy(obj);
            }
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
        /// 延迟归还GameObject到池中
        /// </summary>
        public void DespawnDelayed(GameObject obj, float delay)
        {
            if (obj != null)
            {
                var mono = obj.GetComponent<MonoBehaviour>();
                if (mono != null)
                {
                    mono.StartCoroutine(DespawnCoroutine(obj, delay));
                }
            }
        }

        private System.Collections.IEnumerator DespawnCoroutine(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            Despawn(obj);
        }

        /// <summary>
        /// 清空指定的GameObject对象池
        /// </summary>
        public void ClearGameObjectPool(string poolName)
        {
            if (_gameObjectPools.TryGetValue(poolName, out var pool))
            {
                pool.Clear();
                _gameObjectPools.Remove(poolName);
                Debug.Log($"[PoolModule] 清空GameObject对象池: {poolName}");
            }
        }

        /// <summary>
        /// 清空所有GameObject对象池
        /// </summary>
        public void ClearAllGameObjectPools()
        {
            foreach (var pool in _gameObjectPools.Values)
            {
                pool.Clear();
            }
            _gameObjectPools.Clear();
            Debug.Log("[PoolModule] 清空所有GameObject对象池");
        }

        #endregion

        #region 普通对象池

        /// <summary>
        /// 创建普通对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="createFunc">创建方法</param>
        /// <param name="onGet">获取时回调</param>
        /// <param name="onReturn">归还时回调</param>
        /// <param name="minSize">最小池大小</param>
        /// <param name="maxSize">最大池大小</param>
        /// <returns>对象池</returns>
        public ObjectPool<T> CreateObjectPool<T>(
            Func<T> createFunc = null,
            Action<T> onGet = null,
            Action<T> onReturn = null,
            int minSize = 0,
            int maxSize = 100) where T : class, new()
        {
            var typeName = typeof(T).FullName;

            if (_objectPools.ContainsKey(typeName))
            {
                Debug.LogWarning($"[PoolModule] 对象池已存在: {typeName}");
                return _objectPools[typeName] as ObjectPool<T>;
            }

            var pool = new ObjectPool<T>(createFunc, onGet, onReturn, null, minSize, maxSize);
            _objectPools[typeName] = pool;
            Debug.Log($"[PoolModule] 创建对象池: {typeName}");
            return pool;
        }

        /// <summary>
        /// 获取普通对象池
        /// </summary>
        public ObjectPool<T> GetObjectPool<T>() where T : class, new()
        {
            var typeName = typeof(T).FullName;
            if (_objectPools.TryGetValue(typeName, out var pool))
            {
                return pool as ObjectPool<T>;
            }
            return null;
        }

        /// <summary>
        /// 从对象池获取对象（如果池不存在会自动创建）
        /// </summary>
        public T SpawnObject<T>() where T : class, new()
        {
            var pool = GetObjectPool<T>();
            if (pool == null)
            {
                pool = CreateObjectPool<T>();
            }
            return pool.Spawn();
        }

        /// <summary>
        /// 将对象归还到池中
        /// </summary>
        public void DespawnObject<T>(T obj) where T : class, new()
        {
            var pool = GetObjectPool<T>();
            if (pool != null)
            {
                pool.Despawn(obj);
            }
            else
            {
                Debug.LogWarning($"[PoolModule] 未找到对应的对象池: {typeof(T).FullName}");
            }
        }

        /// <summary>
        /// 清空指定的普通对象池
        /// </summary>
        public void ClearObjectPool<T>() where T : class, new()
        {
            var typeName = typeof(T).FullName;
            if (_objectPools.TryGetValue(typeName, out var pool))
            {
                (pool as ObjectPool<T>)?.Clear();
                _objectPools.Remove(typeName);
                Debug.Log($"[PoolModule] 清空对象池: {typeName}");
            }
        }

        /// <summary>
        /// 清空所有普通对象池
        /// </summary>
        public void ClearAllObjectPools()
        {
            _objectPools.Clear();
            Debug.Log("[PoolModule] 清空所有普通对象池");
        }

        #endregion

        #region 统计信息

        /// <summary>
        /// 获取所有对象池的统计信息
        /// </summary>
        public string GetStatistics()
        {
            var stats = $"[PoolModule] 对象池统计信息:\n";
            stats += $"GameObject池数量: {_gameObjectPools.Count}\n";

            foreach (var kvp in _gameObjectPools)
            {
                var pool = kvp.Value;
                stats += $"  - {kvp.Key}: 总计={pool.TotalCount}, 可用={pool.AvailableCount}, 活跃={pool.ActiveCount}\n";
            }

            stats += $"普通对象池数量: {_objectPools.Count}\n";

            return stats;
        }

        /// <summary>
        /// 打印统计信息到控制台
        /// </summary>
        public void PrintStatistics()
        {
            Debug.Log(GetStatistics());
        }

        #endregion
    }
}

