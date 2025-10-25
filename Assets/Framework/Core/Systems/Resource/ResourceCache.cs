using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.Core.Resource
{
    /// <summary>
    /// 资源缓存项
    /// </summary>
    internal class ResourceCacheItem
    {
        /// <summary>资源路径</summary>
        public string Path { get; set; }

        /// <summary>资源对象</summary>
        public UnityEngine.Object Asset { get; set; }

        /// <summary>引用计数</summary>
        public int ReferenceCount { get; set; }

        /// <summary>最后访问时间</summary>
        public DateTime LastAccessTime { get; set; }

        /// <summary>资源大小（估算，字节）</summary>
        public long Size { get; set; }

        public ResourceCacheItem(string path, UnityEngine.Object asset)
        {
            Path = path;
            Asset = asset;
            ReferenceCount = 1;
            LastAccessTime = DateTime.Now;
            Size = EstimateSize(asset);
        }

        /// <summary>
        /// 估算资源大小
        /// </summary>
        private long EstimateSize(UnityEngine.Object asset)
        {
            if (asset is Texture2D texture)
            {
                return texture.width * texture.height * 4; // RGBA
            }
            else if (asset is AudioClip audio)
            {
                return audio.samples * audio.channels * 2; // 16bit
            }
            else if (asset is GameObject)
            {
                return 10240; // 预估10KB
            }
            else
            {
                return 1024; // 默认1KB
            }
        }
    }

    /// <summary>
    /// 资源缓存管理器
    /// 使用引用计数和LRU策略管理资源缓存
    /// </summary>
    public class ResourceCache
    {
        #region 字段

        /// <summary>缓存字典（路径 -> 缓存项）</summary>
        private readonly Dictionary<string, ResourceCacheItem> _cacheByPath;

        /// <summary>缓存字典（对象 -> 缓存项）</summary>
        private readonly Dictionary<UnityEngine.Object, ResourceCacheItem> _cacheByAsset;

        /// <summary>最大缓存大小（字节）默认100MB</summary>
        private long _maxCacheSize = 100 * 1024 * 1024;

        /// <summary>当前缓存大小（字节）</summary>
        private long _currentCacheSize = 0;

        #endregion

        #region 属性

        /// <summary>
        /// 缓存中的资源数量
        /// </summary>
        public int Count => _cacheByPath.Count;

        /// <summary>
        /// 当前缓存大小（MB）
        /// </summary>
        public float CurrentCacheSizeMB => _currentCacheSize / (1024f * 1024f);

        /// <summary>
        /// 最大缓存大小（MB）
        /// </summary>
        public float MaxCacheSizeMB
        {
            get => _maxCacheSize / (1024f * 1024f);
            set => _maxCacheSize = (long)(value * 1024 * 1024);
        }

        #endregion

        #region 构造函数

        public ResourceCache(float maxCacheSizeMB = 100f)
        {
            _cacheByPath = new Dictionary<string, ResourceCacheItem>();
            _cacheByAsset = new Dictionary<UnityEngine.Object, ResourceCacheItem>();
            MaxCacheSizeMB = maxCacheSizeMB;
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 添加资源到缓存
        /// </summary>
        public void Add(string path, UnityEngine.Object asset)
        {
            if (string.IsNullOrEmpty(path) || asset == null)
                return;

            // 如果已存在，增加引用计数
            if (_cacheByPath.TryGetValue(path, out var item))
            {
                item.ReferenceCount++;
                item.LastAccessTime = DateTime.Now;
                return;
            }

            // 创建新的缓存项
            var cacheItem = new ResourceCacheItem(path, asset);
            _cacheByPath[path] = cacheItem;
            _cacheByAsset[asset] = cacheItem;
            _currentCacheSize += cacheItem.Size;

            // 检查缓存大小，必要时清理
            CheckCacheSize();
        }

        /// <summary>
        /// 根据路径获取资源
        /// </summary>
        public T Get<T>(string path) where T : UnityEngine.Object
        {
            if (_cacheByPath.TryGetValue(path, out var item))
            {
                item.LastAccessTime = DateTime.Now;
                return item.Asset as T;
            }
            return null;
        }

        /// <summary>
        /// 增加引用计数
        /// </summary>
        public void AddReference(string path)
        {
            if (_cacheByPath.TryGetValue(path, out var item))
            {
                item.ReferenceCount++;
                item.LastAccessTime = DateTime.Now;
            }
        }

        /// <summary>
        /// 减少引用计数
        /// </summary>
        public void RemoveReference(string path)
        {
            if (_cacheByPath.TryGetValue(path, out var item))
            {
                item.ReferenceCount = Mathf.Max(0, item.ReferenceCount - 1);
            }
        }

        /// <summary>
        /// 根据对象减少引用计数
        /// </summary>
        public void RemoveReference(UnityEngine.Object asset)
        {
            if (_cacheByAsset.TryGetValue(asset, out var item))
            {
                item.ReferenceCount = Mathf.Max(0, item.ReferenceCount - 1);
            }
        }

        /// <summary>
        /// 获取引用计数
        /// </summary>
        public int GetReferenceCount(string path)
        {
            return _cacheByPath.TryGetValue(path, out var item) ? item.ReferenceCount : 0;
        }

        /// <summary>
        /// 检查资源是否在缓存中
        /// </summary>
        public bool Contains(string path)
        {
            return _cacheByPath.ContainsKey(path);
        }

        /// <summary>
        /// 释放指定资源
        /// </summary>
        public void Release(string path)
        {
            if (_cacheByPath.TryGetValue(path, out var item))
            {
                RemoveFromCache(item);
            }
        }

        /// <summary>
        /// 释放指定资源对象
        /// </summary>
        public void Release(UnityEngine.Object asset)
        {
            if (_cacheByAsset.TryGetValue(asset, out var item))
            {
                RemoveFromCache(item);
            }
        }

        /// <summary>
        /// 释放所有引用计数为0的资源
        /// </summary>
        public void ReleaseUnused()
        {
            var toRemove = new List<ResourceCacheItem>();

            foreach (var item in _cacheByPath.Values)
            {
                if (item.ReferenceCount <= 0)
                {
                    toRemove.Add(item);
                }
            }

            foreach (var item in toRemove)
            {
                RemoveFromCache(item);
            }

            // 卸载未使用的资源
            Resources.UnloadUnusedAssets();
        }

        /// <summary>
        /// 清空所有缓存
        /// </summary>
        public void Clear()
        {
            var items = new List<ResourceCacheItem>(_cacheByPath.Values);
            foreach (var item in items)
            {
                RemoveFromCache(item);
            }

            _cacheByPath.Clear();
            _cacheByAsset.Clear();
            _currentCacheSize = 0;

            Resources.UnloadUnusedAssets();
            GC.Collect();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 从缓存中移除项
        /// </summary>
        private void RemoveFromCache(ResourceCacheItem item)
        {
            if (item == null) return;

            _cacheByPath.Remove(item.Path);
            _cacheByAsset.Remove(item.Asset);
            _currentCacheSize -= item.Size;

            // 释放资源
            if (item.Asset != null && !(item.Asset is GameObject))
            {
                Resources.UnloadAsset(item.Asset);
            }
        }

        /// <summary>
        /// 检查缓存大小，超过限制时使用LRU策略清理
        /// </summary>
        private void CheckCacheSize()
        {
            if (_currentCacheSize <= _maxCacheSize)
                return;

            // 按最后访问时间排序，移除最久未使用的资源
            var sortedItems = new List<ResourceCacheItem>(_cacheByPath.Values);
            sortedItems.Sort((a, b) => a.LastAccessTime.CompareTo(b.LastAccessTime));

            // 移除引用计数为0的最旧资源
            foreach (var item in sortedItems)
            {
                if (item.ReferenceCount <= 0)
                {
                    RemoveFromCache(item);

                    if (_currentCacheSize <= _maxCacheSize * 0.8f) // 清理到80%
                        break;
                }
            }
        }

        #endregion
    }
}

