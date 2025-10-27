using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Framework.Core.Resource
{
    /// <summary>
    /// Addressables资源加载器
    /// 使用Unity的Addressables系统加载资源
    /// 自动检测Addressables是否可用，无需手动定义宏
    /// </summary>
    public class AddressableLoader : IResourceLoader
    {
        #region 字段

        private bool _isAddressablesAvailable;
        private Type _addressablesType;
        private MethodInfo _loadAssetAsyncMethod;
        private MethodInfo _releaseMethod;

        #endregion

        #region 属性

        public string LoaderName => "AddressableLoader";

        /// <summary>
        /// Addressables 是否可用
        /// </summary>
        public bool IsAvailable => _isAddressablesAvailable;

        #endregion

        #region 构造函数

        public AddressableLoader()
        {
            CheckAddressablesAvailability();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 同步加载资源（不推荐）
        /// Addressables不支持真正的同步加载，这里使用异步等待实现
        /// </summary>
        public T Load<T>(string path) where T : UnityEngine.Object
        {
            if (!_isAddressablesAvailable)
            {
                throw new NotSupportedException("[AddressableLoader] Addressables 不可用。请安装 Addressables 包。");
            }

            try
            {
                // 使用反射调用 Addressables.LoadAssetAsync<T>(path)
                var genericMethod = _loadAssetAsyncMethod.MakeGenericMethod(typeof(T));
                var handle = genericMethod.Invoke(null, new object[] { path });

                // 获取 handle.WaitForCompletion() 方法
                var waitMethod = handle.GetType().GetMethod("WaitForCompletion");
                var result = waitMethod.Invoke(handle, null) as T;

                if (result != null)
                {
                    return result;
                }
                else
                {
                    throw new System.IO.FileNotFoundException($"[AddressableLoader] 资源加载失败: {path}");
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                // 直接重新抛出资源未找到异常
                throw;
            }
            catch (Exception e)
            {
                // 包装其他异常
                throw new Exception($"[AddressableLoader] 资源加载异常: {path}", e);
            }
        }

        /// <summary>
        /// 异步加载资源
        /// </summary>
        public async Task<T> LoadAsync<T>(string path, Action<float> onProgress = null) where T : UnityEngine.Object
        {
            if (!_isAddressablesAvailable)
            {
                throw new NotSupportedException("[AddressableLoader] Addressables 不可用。请安装 Addressables 包。");
            }

            try
            {
                // 使用反射调用 Addressables.LoadAssetAsync<T>(path)
                var genericMethod = _loadAssetAsyncMethod.MakeGenericMethod(typeof(T));
                var handle = genericMethod.Invoke(null, new object[] { path });

                // 获取 IsDone 和 PercentComplete 属性
                var isDoneProperty = handle.GetType().GetProperty("IsDone");
                var percentProperty = handle.GetType().GetProperty("PercentComplete");
                var statusProperty = handle.GetType().GetProperty("Status");
                var resultProperty = handle.GetType().GetProperty("Result");

                // 等待加载完成
                while (!(bool)isDoneProperty.GetValue(handle))
                {
                    float progress = (float)percentProperty.GetValue(handle);
                    onProgress?.Invoke(progress);
                    await Task.Yield();
                }

                onProgress?.Invoke(1f);

                // 检查状态（Status == AsyncOperationStatus.Succeeded）
                var status = (int)statusProperty.GetValue(handle);
                if (status == 1) // Succeeded
                {
                    return resultProperty.GetValue(handle) as T;
                }
                else
                {
                    throw new System.IO.FileNotFoundException($"[AddressableLoader] 资源加载失败: {path}");
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                // 直接重新抛出资源未找到异常
                throw;
            }
            catch (Exception e)
            {
                // 包装其他异常
                throw new Exception($"[AddressableLoader] 资源加载异常: {path}", e);
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Release(UnityEngine.Object asset)
        {
            if (!_isAddressablesAvailable)
            {
                return;
            }

            try
            {
                if (asset != null)
                {
                    _releaseMethod.Invoke(null, new object[] { asset });
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[AddressableLoader] 资源释放异常: {e}");
            }
        }

        /// <summary>
        /// 检查资源是否存在
        /// </summary>
        public bool Exists(string path)
        {
            if (!_isAddressablesAvailable)
            {
                return false;
            }

            // Addressables 没有直接的 Exists 方法
            // 实际项目中应该维护一个资源清单或使用 Addressables.LoadResourceLocationsAsync
            return true; // 简化实现
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 检查 Addressables 是否可用
        /// </summary>
        private void CheckAddressablesAvailability()
        {
            try
            {
                // 尝试查找 Addressables 类型
                _addressablesType = Type.GetType("UnityEngine.AddressableAssets.Addressables, Unity.Addressables");

                if (_addressablesType != null)
                {
                    // 获取常用方法
                    _loadAssetAsyncMethod = _addressablesType.GetMethod(
                        "LoadAssetAsync",
                        BindingFlags.Public | BindingFlags.Static,
                        null,
                        new Type[] { typeof(object) },
                        null
                    );

                    _releaseMethod = _addressablesType.GetMethod(
                        "Release",
                        BindingFlags.Public | BindingFlags.Static,
                        null,
                        new Type[] { typeof(object) },
                        null
                    );

                    _isAddressablesAvailable = _loadAssetAsyncMethod != null && _releaseMethod != null;

                    if (_isAddressablesAvailable)
                    {
                        Debug.Log("[AddressableLoader] Addressables 系统可用");
                    }
                    else
                    {
                        Debug.LogWarning("[AddressableLoader] 找到 Addressables 类型但方法不完整");
                    }
                }
                else
                {
                    _isAddressablesAvailable = false;
                    Debug.LogWarning("[AddressableLoader] 未检测到 Addressables 包。如需使用 Addressables，请通过 Package Manager 安装。");
                }
            }
            catch (Exception e)
            {
                _isAddressablesAvailable = false;
                Debug.LogWarning($"[AddressableLoader] 检测 Addressables 失败: {e.Message}");
            }
        }

        #endregion
    }
}

