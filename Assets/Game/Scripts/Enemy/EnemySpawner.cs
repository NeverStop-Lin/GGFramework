using System.Collections.Generic;
using Framework.Core;
using Framework.Scripts;
using UnityEngine;

namespace Game.Enemy
{
    /// <summary>
    /// 敌人生成器
    /// 定时在场景中生成敌人
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("生成设置")]
        [SerializeField] private List<GameObject> enemyPrefabs;
        [SerializeField] private float spawnInterval = 15f;
        [SerializeField] private int maxEnemyCount = 10;
        [SerializeField] private float spawnRadius = 30f;
        [SerializeField] private float minDistanceFromPlayer = 15f;

        [Header("生成位置")]
        [SerializeField] private Transform playerTransform;
        
        private List<GameObject> _spawnedEnemies = new List<GameObject>();
        private Timer _spawnTimer;
        private bool _isRunning;

        private void Start()
        {
            if (playerTransform == null)
            {
                var playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    playerTransform = playerObject.transform;
                }
            }
        }

        /// <summary>
        /// 启动生成器
        /// </summary>
        public void StartSpawner()
        {
            if (_isRunning) return;
            
            _isRunning = true;
            
            // 立即生成第一波敌人
            SpawnEnemy();
            
            // 设置定时生成（每隔spawnInterval秒执行，无限次）
            _spawnTimer = GridFramework.Timer.Interval(() =>
            {
                // 清理已销毁的敌人引用
                _spawnedEnemies.RemoveAll(e => e == null);
                
                // 检查敌人数量
                if (_spawnedEnemies.Count < maxEnemyCount)
                {
                    SpawnEnemy();
                }
            }, spawnInterval, -1);
            
            Debug.Log("敌人生成器已启动");
        }

        /// <summary>
        /// 停止生成器
        /// </summary>
        public void StopSpawner()
        {
            if (!_isRunning) return;
            
            _isRunning = false;
            
            _spawnTimer?.Stop();
            
            Debug.Log("敌人生成器已停止");
        }

        /// <summary>
        /// 生成一个敌人
        /// </summary>
        private void SpawnEnemy()
        {
            if (enemyPrefabs == null || enemyPrefabs.Count == 0)
            {
                Debug.LogWarning("敌人预制体列表为空");
                return;
            }

            if (playerTransform == null)
            {
                Debug.LogWarning("找不到玩家对象");
                return;
            }

            // 随机选择敌人类型
            int randomIndex = Random.Range(0, enemyPrefabs.Count);
            GameObject prefab = enemyPrefabs[randomIndex];

            if (prefab == null)
            {
                Debug.LogWarning($"敌人预制体[{randomIndex}]为空");
                return;
            }

            // 获取生成位置
            Vector3 spawnPosition = GetRandomSpawnPosition();
            
            // 实例化敌人
            GameObject enemyObject = Instantiate(prefab, spawnPosition, Quaternion.identity, transform);
            _spawnedEnemies.Add(enemyObject);
            
            Debug.Log($"生成敌人，当前数量：{_spawnedEnemies.Count}");
        }

        /// <summary>
        /// 获取随机生成位置（在玩家周围，但保持最小距离）
        /// </summary>
        private Vector3 GetRandomSpawnPosition()
        {
            Vector3 playerPosition = playerTransform.position;
            Vector3 spawnPosition;
            int attemptCount = 0;
            const int maxAttempts = 10;

            do
            {
                // 在圆环范围内随机生成
                Vector2 randomDirection = Random.insideUnitCircle.normalized;
                float randomDistance = Random.Range(minDistanceFromPlayer, spawnRadius);
                
                spawnPosition = playerPosition + new Vector3(
                    randomDirection.x * randomDistance,
                    0f,
                    randomDirection.y * randomDistance
                );
                
                attemptCount++;
                
            } while (Vector3.Distance(spawnPosition, playerPosition) < minDistanceFromPlayer && attemptCount < maxAttempts);

            return spawnPosition;
        }

        /// <summary>
        /// 清除所有敌人
        /// </summary>
        public void ClearAllEnemies()
        {
            foreach (var enemy in _spawnedEnemies)
            {
                if (enemy != null)
                {
                    Destroy(enemy);
                }
            }
            
            _spawnedEnemies.Clear();
            Debug.Log("所有敌人已清除");
        }

        private void OnDestroy()
        {
            StopSpawner();
        }

        #region 调试绘制

        private void OnDrawGizmosSelected()
        {
            if (playerTransform == null)
            {
                var playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    playerTransform = playerObject.transform;
                }
            }

            if (playerTransform != null)
            {
                // 绘制最小距离
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(playerTransform.position, minDistanceFromPlayer);

                // 绘制生成半径
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(playerTransform.position, spawnRadius);
            }
        }

        #endregion
    }
}
