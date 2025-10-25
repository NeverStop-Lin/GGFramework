using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Framework.Core
{
    /// <summary>
    /// 资源管理接口
    /// 提供统一的资源加载、缓存和释放功能
    /// </summary>
    public interface IResource
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
        /// <returns>加载的资源对象</returns>
        Task<T> LoadAsync<T>(string path) where T : UnityEngine.Object;

        /// <summary>
        /// 异步加载资源（带进度回调）
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="path">资源路径</param>
        /// <param name="onProgress">进度回调（0-1）</param>
        /// <returns>加载的资源对象</returns>
        Task<T> LoadAsync<T>(string path, Action<float> onProgress) where T : UnityEngine.Object;

        /// <summary>
        /// 批量预加载资源
        /// </summary>
        /// <param name="paths">资源路径数组</param>
        /// <returns>预加载任务</returns>
        Task PreloadAsync(params string[] paths);

        /// <summary>
        /// 批量预加载资源（带进度回调）
        /// </summary>
        /// <param name="onProgress">进度回调（0-1）</param>
        /// <param name="paths">资源路径数组</param>
        /// <returns>预加载任务</returns>
        Task PreloadAsync(Action<float> onProgress, params string[] paths);

        /// <summary>
        /// 释放指定路径的资源
        /// </summary>
        /// <param name="path">资源路径</param>
        void Release(string path);

        /// <summary>
        /// 释放指定的资源对象
        /// </summary>
        /// <param name="asset">资源对象</param>
        void Release(UnityEngine.Object asset);

        /// <summary>
        /// 释放所有未使用的资源
        /// </summary>
        void ReleaseUnused();

        /// <summary>
        /// 清空所有缓存（强制释放所有资源）
        /// </summary>
        void ClearCache();

        /// <summary>
        /// 检查资源是否已加载
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns>是否已加载</returns>
        bool IsLoaded(string path);

        /// <summary>
        /// 获取资源的引用计数
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns>引用计数</returns>
        int GetReferenceCount(string path);

        /// <summary>
        /// 获取缓存中的资源数量
        /// </summary>
        int CacheCount { get; }
    }
}

