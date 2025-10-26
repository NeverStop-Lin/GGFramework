using System.Collections.Generic;
using UnityEngine;

namespace Game.Resource
{
    /// <summary>
    /// 资源生成器
    /// 在场景中随机生成资源对象
    /// </summary>
    public class ResourceSpawner : MonoBehaviour
    {
        [Header("生成设置")]
        [SerializeField] private List<GameObject> resourcePrefabs;
        [SerializeField] private int initialSpawnCount = 20;
        [SerializeField] private float spawnRadius = 50f;
        [SerializeField] private LayerMask groundLayer;

        [Header("生成位置")]
        [SerializeField] private Vector3 spawnCenter = Vector3.zero;

        private List<GameObject> _spawnedResources = new List<GameObject>();

        private void Start()
        {
            SpawnInitialResources();
        }

        /// <summary>
        /// 生成初始资源
        /// </summary>
        private void SpawnInitialResources()
        {
            if (resourcePrefabs == null || resourcePrefabs.Count == 0)
            {
                Debug.LogWarning("资源预制体列表为空，无法生成资源");
                return;
            }

            for (int i = 0; i < initialSpawnCount; i++)
            {
                SpawnRandomResource();
            }

            Debug.Log($"已生成 {initialSpawnCount} 个资源对象");
        }

        /// <summary>
        /// 生成一个随机资源
        /// </summary>
        private void SpawnRandomResource()
        {
            // 随机选择资源类型
            int randomIndex = Random.Range(0, resourcePrefabs.Count);
            GameObject prefab = resourcePrefabs[randomIndex];

            if (prefab == null)
            {
                Debug.LogWarning($"资源预制体[{randomIndex}]为空");
                return;
            }

            // 随机生成位置
            Vector3 spawnPosition = GetRandomPosition();
            
            // 实例化资源
            GameObject resourceObject = Instantiate(prefab, spawnPosition, Quaternion.identity, transform);
            _spawnedResources.Add(resourceObject);
        }

        /// <summary>
        /// 获取随机生成位置
        /// </summary>
        private Vector3 GetRandomPosition()
        {
            // 在圆形范围内随机生成
            Vector2 randomPoint = Random.insideUnitCircle * spawnRadius;
            Vector3 position = spawnCenter + new Vector3(randomPoint.x, 100f, randomPoint.y);

            // 射线检测找到地面
            if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, 200f, groundLayer))
            {
                return hit.point;
            }

            // 如果没有检测到地面，使用默认高度
            return new Vector3(position.x, 0f, position.z);
        }

        /// <summary>
        /// 清除所有已生成的资源
        /// </summary>
        public void ClearAllResources()
        {
            foreach (var resource in _spawnedResources)
            {
                if (resource != null)
                {
                    Destroy(resource);
                }
            }
            
            _spawnedResources.Clear();
            Debug.Log("所有资源已清除");
        }

        #region 调试绘制

        private void OnDrawGizmosSelected()
        {
            // 绘制生成范围
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(spawnCenter, spawnRadius);
        }

        #endregion
    }
}
