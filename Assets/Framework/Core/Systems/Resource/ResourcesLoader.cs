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
                // 返回null表示资源不存在，不是异常（Unity的正常行为）
                return asset;
            }
            catch (Exception e)
            {
                // 加载过程中的异常（如路径格式错误等）需要抛出
                throw new Exception($"[ResourcesLoader] 资源加载异常: {path}", e);
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

                // 返回null表示资源不存在，不是异常（Unity的正常行为）
                return request.asset as T;
            }
            catch (Exception e)
            {
                // 加载过程中的异常（如路径格式错误等）需要抛出
                throw new Exception($"[ResourcesLoader] 资源加载异常: {path}", e);
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

