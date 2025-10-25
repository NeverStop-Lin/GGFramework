using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.Core.Resource
{
    /// <summary>
    /// Addressable 资源清单
    /// 存储所有被标记为 Addressable 的资源路径
    /// </summary>
    [CreateAssetMenu(fileName = "AddressableResourceManifest", menuName = "Framework/Resource/Addressable Manifest")]
    public class AddressableResourceManifest : ScriptableObject
    {
        #region 数据结构

        [Serializable]
        public class ResourceEntry
        {
            /// <summary>资源地址（Address）</summary>
            public string address;

            /// <summary>资源类型</summary>
            public string assetType;

            /// <summary>GUID</summary>
            public string guid;

            /// <summary>物理路径</summary>
            public string physicalPath;

            /// <summary>所属分组</summary>
            public string groupName;
        }

        #endregion

        #region 字段

        [Header("清单信息")]
        [Tooltip("生成时间")]
        public string generatedTime;

        [Tooltip("资源总数")]
        public int totalCount;

        [Header("资源列表")]
        [Tooltip("所有 Addressable 资源")]
        public List<ResourceEntry> resources = new List<ResourceEntry>();

        // 运行时快速查找表（不序列化）
        [NonSerialized]
        private HashSet<string> _addressHashSet;

        [NonSerialized]
        private Dictionary<string, ResourceEntry> _addressDictionary;

        [NonSerialized]
        private bool _initialized = false;

        #endregion

        #region 公共方法

        /// <summary>
        /// 初始化快速查找表
        /// </summary>
        public void Initialize()
        {
            if (_initialized)
                return;

            _addressHashSet = new HashSet<string>();
            _addressDictionary = new Dictionary<string, ResourceEntry>();

            foreach (var entry in resources)
            {
                if (!string.IsNullOrEmpty(entry.address))
                {
                    _addressHashSet.Add(entry.address);
                    _addressDictionary[entry.address] = entry;
                }
            }

            _initialized = true;
            Debug.Log($"[AddressableResourceManifest] 清单初始化完成，共 {totalCount} 个资源");
        }

        /// <summary>
        /// 检查资源是否在 Addressables 中
        /// </summary>
        public bool Contains(string address)
        {
            if (!_initialized)
                Initialize();

            return _addressHashSet.Contains(address);
        }

        /// <summary>
        /// 获取资源条目
        /// </summary>
        public ResourceEntry GetEntry(string address)
        {
            if (!_initialized)
                Initialize();

            return _addressDictionary.TryGetValue(address, out var entry) ? entry : null;
        }

        /// <summary>
        /// 获取指定分组的所有资源
        /// </summary>
        public List<string> GetResourcesByGroup(string groupName)
        {
            var result = new List<string>();
            foreach (var entry in resources)
            {
                if (entry.groupName == groupName)
                {
                    result.Add(entry.address);
                }
            }
            return result;
        }

        /// <summary>
        /// 获取指定类型的所有资源
        /// </summary>
        public List<string> GetResourcesByType(string assetType)
        {
            var result = new List<string>();
            foreach (var entry in resources)
            {
                if (entry.assetType == assetType)
                {
                    result.Add(entry.address);
                }
            }
            return result;
        }

        /// <summary>
        /// 清空清单
        /// </summary>
        public void Clear()
        {
            resources.Clear();
            totalCount = 0;
            _addressHashSet?.Clear();
            _addressDictionary?.Clear();
            _initialized = false;
        }

        #endregion

        #region 编辑器方法

#if UNITY_EDITOR
        /// <summary>
        /// 添加资源条目（编辑器用）
        /// </summary>
        public void AddResource(string address, string assetType, string guid, string physicalPath, string groupName)
        {
            var entry = new ResourceEntry
            {
                address = address,
                assetType = assetType,
                guid = guid,
                physicalPath = physicalPath,
                groupName = groupName
            };

            resources.Add(entry);
            totalCount = resources.Count;
        }

        /// <summary>
        /// 设置生成时间（编辑器用）
        /// </summary>
        public void SetGeneratedTime()
        {
            generatedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
#endif

        #endregion
    }
}

