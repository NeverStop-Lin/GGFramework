using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Framework.Core.Resource
{
    /// <summary>
    /// 资源加载器类型
    /// </summary>
    public enum ResourceLoaderType
    {
        /// <summary>使用Resources加载</summary>
        Resources,
        /// <summary>使用Addressables加载</summary>
        Addressables
    }

    /// <summary>
    /// 资源管理中心
    /// 统一管理资源的加载、缓存和释放
    /// </summary>
    public class ResourceCenter : IResource
    {
        #region 字段

        /// <summary>资源缓存管理器</summary>
        private readonly ResourceCache _cache;

        /// <summary>当前使用的资源加载器</summary>
        private IResourceLoader _loader;

        /// <summary>可用的加载器字典</summary>
        private readonly Dictionary<ResourceLoaderType, IResourceLoader> _loaders;

        /// <summary>当前加载器类型</summary>
        private ResourceLoaderType _currentLoaderType;

        #endregion

        #region 属性

        /// <summary>
        /// 缓存中的资源数量
        /// </summary>
        public int CacheCount => _cache.Count;

        /// <summary>
        /// 当前使用的加载器类型
        /// </summary>
        public ResourceLoaderType CurrentLoaderType
        {
            get => _currentLoaderType;
            set
            {
                if (_loaders.TryGetValue(value, out var loader))
                {
                    _currentLoaderType = value;
                    _loader = loader;
                    Debug.Log($"[ResourceCenter] 切换资源加载器: {_loader.LoaderName}");
                }
                else
                {
                    Debug.LogError($"[ResourceCenter] 加载器类型不存在: {value}");
                }
            }
        }

        /// <summary>
        /// 最大缓存大小（MB）
        /// </summary>
        public float MaxCacheSizeMB
        {
            get => _cache.MaxCacheSizeMB;
            set => _cache.MaxCacheSizeMB = value;
        }

        /// <summary>
        /// 当前缓存大小（MB）
        /// </summary>
        public float CurrentCacheSizeMB => _cache.CurrentCacheSizeMB;

        #endregion

        #region 构造函数

        [Inject]
        public ResourceCenter()
        {
            _cache = new ResourceCache(100f); // 默认100MB缓存
            _loaders = new Dictionary<ResourceLoaderType, IResourceLoader>();

            // 注册Resources加载器（始终可用）
            RegisterLoader(ResourceLoaderType.Resources, new ResourcesLoader());

            // 注册Addressables加载器（自动检测）
            var addressableLoader = new AddressableLoader();
            RegisterLoader(ResourceLoaderType.Addressables, addressableLoader);

            // 智能选择加载器：如果 Addressables 可用则优先使用，否则使用 Resources
            if (addressableLoader.IsAvailable)
            {
                CurrentLoaderType = ResourceLoaderType.Addressables;
                Debug.Log("[ResourceCenter] 检测到 Addressables，默认使用 Addressables 加载器");
            }
            else
            {
                CurrentLoaderType = ResourceLoaderType.Resources;
                Debug.Log("[ResourceCenter] 使用 Resources 加载器（可通过 Package Manager 安装 Addressables 获得更好的性能）");
            }

            Debug.Log("[ResourceCenter] 资源管理系统初始化完成");
        }

        #endregion

        #region 公共方法 - 资源加载

        /// <summary>
        /// 同步加载资源
        /// </summary>
        public T Load<T>(string path) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("[ResourceCenter] 资源路径为空");
            }

            // 先从缓存获取
            var cachedAsset = _cache.Get<T>(path);
            if (cachedAsset != null)
            {
                _cache.AddReference(path);
                return cachedAsset;
            }

            // 使用加载器加载（加载器会在失败时抛出异常）
            var asset = _loader.Load<T>(path);
            if (asset != null)
            {
                _cache.Add(path, asset);
            }

            return asset;
        }

        /// <summary>
        /// 异步加载资源
        /// </summary>
        public async Task<T> LoadAsync<T>(string path) where T : UnityEngine.Object
        {
            return await LoadAsync<T>(path, null);
        }

        /// <summary>
        /// 异步加载资源（带进度回调）
        /// </summary>
        public async Task<T> LoadAsync<T>(string path, Action<float> onProgress) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("[ResourceCenter] 资源路径为空");
            }

            // 先从缓存获取
            var cachedAsset = _cache.Get<T>(path);
            if (cachedAsset != null)
            {
                _cache.AddReference(path);
                onProgress?.Invoke(1f);
                return cachedAsset;
            }

            // 使用加载器加载（加载器会在失败时抛出异常）
            var asset = await _loader.LoadAsync<T>(path, onProgress);
            if (asset != null)
            {
                _cache.Add(path, asset);
            }

            return asset;
        }

        /// <summary>
        /// 批量预加载资源
        /// </summary>
        public async Task PreloadAsync(params string[] paths)
        {
            await PreloadAsync(null, paths);
        }

        /// <summary>
        /// 批量预加载资源（带进度回调）
        /// </summary>
        public async Task PreloadAsync(Action<float> onProgress, params string[] paths)
        {
            if (paths == null || paths.Length == 0)
                return;

            int totalCount = paths.Length;
            int loadedCount = 0;

            foreach (var path in paths)
            {
                await LoadAsync<UnityEngine.Object>(path, null);
                loadedCount++;
                onProgress?.Invoke((float)loadedCount / totalCount);
            }
        }

        #endregion

        #region 公共方法 - 资源释放

        /// <summary>
        /// 释放指定路径的资源
        /// </summary>
        public void Release(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            _cache.RemoveReference(path);
        }

        /// <summary>
        /// 释放指定的资源对象
        /// </summary>
        public void Release(UnityEngine.Object asset)
        {
            if (asset == null)
                return;

            _cache.RemoveReference(asset);
        }

        /// <summary>
        /// 释放所有未使用的资源（引用计数为0）
        /// </summary>
        public void ReleaseUnused()
        {
            _cache.ReleaseUnused();
            Debug.Log("[ResourceCenter] 已释放未使用的资源");
        }

        /// <summary>
        /// 清空所有缓存（强制释放所有资源）
        /// </summary>
        public void ClearCache()
        {
            _cache.Clear();
            Debug.Log("[ResourceCenter] 已清空所有资源缓存");
        }

        #endregion

        #region 公共方法 - 查询

        /// <summary>
        /// 检查资源是否已加载
        /// </summary>
        public bool IsLoaded(string path)
        {
            return _cache.Contains(path);
        }

        /// <summary>
        /// 获取资源的引用计数
        /// </summary>
        public int GetReferenceCount(string path)
        {
            return _cache.GetReferenceCount(path);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 注册资源加载器
        /// </summary>
        private void RegisterLoader(ResourceLoaderType type, IResourceLoader loader)
        {
            if (loader != null)
            {
                _loaders[type] = loader;
                Debug.Log($"[ResourceCenter] 注册资源加载器: {loader.LoaderName}");
            }
        }

        #endregion
    }
}

