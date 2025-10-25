using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Framework.Core
{
    /// <summary>
    /// 资源加载器接口
    /// 定义资源加载的具体实现方式
    /// </summary>
    public interface IResourceLoader
    {
        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="path">资源路径</param>
        /// <returns>加载的资源对象</returns>
        T Load<T>(string path) where T : UnityEngine.Object;

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="path">资源路径</param>
        /// <param name="onProgress">进度回调（0-1）</param>
        /// <returns>加载的资源对象</returns>
        Task<T> LoadAsync<T>(string path, Action<float> onProgress = null) where T : UnityEngine.Object;

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="asset">资源对象</param>
        void Release(UnityEngine.Object asset);

        /// <summary>
        /// 检查资源是否存在
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns>是否存在</returns>
        bool Exists(string path);

        /// <summary>
        /// 获取加载器名称
        /// </summary>
        string LoaderName { get; }

        /// <summary>
        /// 加载器是否可用
        /// </summary>
        bool IsAvailable { get; }
    }
}

