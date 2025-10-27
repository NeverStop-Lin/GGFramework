using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Framework.Core.Resource
{
    /// <summary>
    /// 自动资源加载器
    /// 通过资源清单快速判断资源位置，智能选择加载方式
    /// 
    /// 优先级：
    /// 1. 清单查询（0ms）⭐⭐⭐ - 最快，直接查表
    /// 2. 试错模式（0.1-5ms）⭐⭐ - 降级方案，先 Resources 后 Addressables
    /// </summary>
    public class AutoResourceLoader : IResourceLoader
    {
        #region 字段

        private ResourcesLoader _resourcesLoader;
        private AddressableLoader _addressableLoader;
        
        // 资源清单（快速查询）⭐
        private AddressableResourceManifest _manifest;
        private bool _manifestLoaded = false;
        
        // 缓存：路径 -> 是否在 Addressables 中
        private readonly Dictionary<string, bool> _addressableCheckCache = 
            new Dictionary<string, bool>();

        // Addressables 可用性
        private bool _addressablesAvailable;

        #endregion

        #region 属性

        public string LoaderName => "AutoResourceLoader";

        public bool IsAvailable => true; // 总是可用（会降级到 Resources）

        /// <summary>
        /// Addressables 是否可用
        /// </summary>
        public bool AddressablesAvailable => _addressablesAvailable;

        /// <summary>
        /// 清单是否已加载
        /// </summary>
        public bool ManifestLoaded => _manifestLoaded;

        /// <summary>
        /// 清单中的资源数量
        /// </summary>
        public int ManifestResourceCount => _manifestLoaded ? _manifest.totalCount : 0;

        #endregion

        #region 构造函数

        public AutoResourceLoader()
        {
            _resourcesLoader = new ResourcesLoader();
            _addressableLoader = new AddressableLoader();
            
            _addressablesAvailable = _addressableLoader.IsAvailable;
            
            // 加载 Addressable 资源清单 ⭐
            LoadManifest();

            Debug.Log($"[AutoResourceLoader] 自动资源加载器初始化完成");
            Debug.Log($"  - Addressables 可用: {_addressablesAvailable}");
            Debug.Log($"  - 资源清单: {(_manifestLoaded ? $"已加载（{_manifest.totalCount} 个资源）" : "未找到（将使用试错模式）")}");
            
            if (_manifestLoaded)
            {
                Debug.Log($"  - 查询方式: 清单查询（0ms）⭐⭐⭐");
            }
            else
            {
                Debug.Log($"  - 查询方式: 试错模式（先 Resources 0.1ms，后 Addressables 5ms）⭐⭐");
                Debug.LogWarning($"  - 建议：Tools → 资源管理 → 生成 Addressable 清单");
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 同步加载资源（自动选择）
        /// </summary>
        public T Load<T>(string path) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("[AutoResourceLoader] 资源路径为空");
            }

            // 策略1：通过清单快速判断（0ms）⭐⭐⭐
            if (_manifestLoaded && _manifest.Contains(path))
            {
                Debug.Log($"[AutoResourceLoader] 清单命中，使用 Addressables: {path}");
                return _addressableLoader.Load<T>(path);
            }

            // 策略2：检查缓存
            if (_addressableCheckCache.TryGetValue(path, out bool isAddressable))
            {
                if (isAddressable)
                {
                    Debug.Log($"[AutoResourceLoader] 缓存命中，使用 Addressables: {path}");
                    return _addressableLoader.Load<T>(path);
                }
                else
                {
                    Debug.Log($"[AutoResourceLoader] 缓存命中，使用 Resources: {path}");
                    return _resourcesLoader.Load<T>(path);
                }
            }

            // 策略3：试错模式（降级方案）⚡
            Debug.Log($"[AutoResourceLoader] 试错模式: {path}");
            
            // 先尝试 Resources（快速，0.1ms）
            var asset = _resourcesLoader.Load<T>(path);
            
            if (asset != null)
            {
                Debug.Log($"[AutoResourceLoader] ✅ Resources 加载成功: {path}");
                _addressableCheckCache[path] = false;
                return asset;
            }
            
            // 再尝试 Addressables（较慢，5-10ms）
            if (_addressablesAvailable)
            {
                Debug.Log($"[AutoResourceLoader] Resources 未找到，尝试 Addressables: {path}");
                asset = _addressableLoader.Load<T>(path);
                
                if (asset != null)
                {
                    Debug.Log($"[AutoResourceLoader] ✅ Addressables 加载成功: {path}");
                    _addressableCheckCache[path] = true;
                    return asset;
                }
            }
            
            // 所有加载方式都失败，抛出异常
            throw new System.IO.FileNotFoundException($"[AutoResourceLoader] 无法加载资源: {path}，已尝试 Resources 和 Addressables");
        }

        /// <summary>
        /// 异步加载资源（自动选择）
        /// </summary>
        public async Task<T> LoadAsync<T>(string path, Action<float> onProgress = null) 
            where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("[AutoResourceLoader] 资源路径为空");
            }

            // 策略1：通过清单快速判断（0ms）⭐⭐⭐
            if (_manifestLoaded && _manifest.Contains(path))
            {
                Debug.Log($"[AutoResourceLoader] 清单命中，使用 Addressables: {path}");
                return await _addressableLoader.LoadAsync<T>(path, onProgress);
            }

            // 策略2：检查缓存
            if (_addressableCheckCache.TryGetValue(path, out bool isAddressable))
            {
                if (isAddressable)
                {
                    Debug.Log($"[AutoResourceLoader] 缓存命中，使用 Addressables: {path}");
                    return await _addressableLoader.LoadAsync<T>(path, onProgress);
                }
                else
                {
                    Debug.Log($"[AutoResourceLoader] 缓存命中，使用 Resources: {path}");
                    return await _resourcesLoader.LoadAsync<T>(path, onProgress);
                }
            }

            // 策略3：试错模式（降级方案）⚡
            Debug.Log($"[AutoResourceLoader] 试错模式: {path}");
            
            // 先尝试 Resources（快速，0.1ms）
            var asset = await _resourcesLoader.LoadAsync<T>(path, onProgress);
            
            if (asset != null)
            {
                Debug.Log($"[AutoResourceLoader] ✅ Resources 加载成功: {path}");
                _addressableCheckCache[path] = false;
                return asset;
            }
            
            // 再尝试 Addressables（较慢，5-10ms）
            if (_addressablesAvailable)
            {
                Debug.Log($"[AutoResourceLoader] Resources 未找到，尝试 Addressables: {path}");
                asset = await _addressableLoader.LoadAsync<T>(path, onProgress);
                
                if (asset != null)
                {
                    Debug.Log($"[AutoResourceLoader] ✅ Addressables 加载成功: {path}");
                    _addressableCheckCache[path] = true;
                    return asset;
                }
            }
            
            // 所有加载方式都失败，抛出异常
            throw new System.IO.FileNotFoundException($"[AutoResourceLoader] 无法加载资源: {path}，已尝试 Resources 和 Addressables");
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Release(UnityEngine.Object asset)
        {
            // 两种加载器都尝试释放
            _addressableLoader.Release(asset);
            _resourcesLoader.Release(asset);
        }

        /// <summary>
        /// 检查资源是否存在
        /// </summary>
        public bool Exists(string path)
        {
            // 先检查清单
            if (_manifestLoaded && _manifest.Contains(path))
                return true;

            // 再检查 Resources
            return _resourcesLoader.Exists(path);
        }

        /// <summary>
        /// 重新加载清单（用于运行时更新）
        /// </summary>
        public void ReloadManifest()
        {
            _addressableCheckCache.Clear();
            LoadManifest();
        }

        /// <summary>
        /// 清除试错缓存
        /// </summary>
        public void ClearCache()
        {
            _addressableCheckCache.Clear();
            Debug.Log("[AutoResourceLoader] 试错缓存已清除");
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 加载 Addressable 资源清单
        /// </summary>
        private void LoadManifest()
        {
            try
            {
                // 从 Resources 加载清单（清单放在 Generations/Resources/ 目录）
                _manifest = Resources.Load<AddressableResourceManifest>("AddressableResourceManifest");

                if (_manifest != null)
                {
                    _manifest.Initialize();
                    _manifestLoaded = true;
                    Debug.Log($"[AutoResourceLoader] ✅ 资源清单加载成功");
                    Debug.Log($"  - 清单资源数: {_manifest.totalCount}");
                    Debug.Log($"  - 生成时间: {_manifest.generatedTime}");
                    Debug.Log($"  - 查询性能: 0ms（哈希表查询）⭐");
                }
                else
                {
                    _manifestLoaded = false;
                    Debug.LogWarning($"[AutoResourceLoader] ⚠️ 资源清单未找到");
                    Debug.LogWarning($"  - 将使用试错模式（先 Resources 后 Addressables）");
                    Debug.LogWarning($"  - 建议生成清单以获得最佳性能:");
                    Debug.LogWarning($"    Tools → 资源管理 → 生成 Addressable 清单");
                }
            }
            catch (Exception e)
            {
                _manifestLoaded = false;
                Debug.LogWarning($"[AutoResourceLoader] 加载资源清单失败: {e.Message}");
                Debug.LogWarning($"  - 将使用试错模式");
            }
        }

        #endregion
    }
}

