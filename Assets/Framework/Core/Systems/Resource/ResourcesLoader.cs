using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Framework.Core.Resource
{
    /// <summary>
    /// Resources资源加载器
    /// 使用Unity的Resources.Load加载资源
    /// </summary>
    public class ResourcesLoader : IResourceLoader
    {
        #region 属性

        public string LoaderName => "ResourcesLoader";

        /// <summary>
        /// Resources 加载器始终可用
        /// </summary>
        public bool IsAvailable => true;

        #endregion

        #region 公共方法

        /// <summary>
        /// 同步加载资源
        /// </summary>
        public T Load<T>(string path) where T : UnityEngine.Object
        {
            try
            {
                var asset = Resources.Load<T>(path);
                if (asset == null)
                {
                    Debug.LogWarning($"[ResourcesLoader] 资源加载失败: {path}");
                }
                return asset;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ResourcesLoader] 资源加载异常: {path}\n{e}");
                return null;
            }
        }

        /// <summary>
        /// 异步加载资源
        /// </summary>
        public async Task<T> LoadAsync<T>(string path, Action<float> onProgress = null) where T : UnityEngine.Object
        {
            try
            {
                var request = Resources.LoadAsync<T>(path);

                while (!request.isDone)
                {
                    onProgress?.Invoke(request.progress);
                    await Task.Yield();
                }

                onProgress?.Invoke(1f);

                if (request.asset == null)
                {
                    Debug.LogWarning($"[ResourcesLoader] 资源加载失败: {path}");
                }

                return request.asset as T;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ResourcesLoader] 资源加载异常: {path}\n{e}");
                return null;
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Release(UnityEngine.Object asset)
        {
            if (asset != null && !(asset is GameObject))
            {
                Resources.UnloadAsset(asset);
            }
        }

        /// <summary>
        /// 检查资源是否存在
        /// </summary>
        public bool Exists(string path)
        {
            var asset = Resources.Load(path);
            if (asset != null)
            {
                if (!(asset is GameObject))
                {
                    Resources.UnloadAsset(asset);
                }
                return true;
            }
            return false;
        }

        #endregion
    }
}

