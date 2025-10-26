using System.Collections.Generic;
using Framework.Scripts;
using Game.Enemy;
using Game.Player;
using Game.Resource;
using Game.UI;
using UnityEngine;
using ResourceConfig = Generate.Scripts.Configs.ResourceConfig;
using EnemyConfig = Generate.Scripts.Configs.EnemyConfig;

namespace Game.Manager
{
    /// <summary>
    /// 游戏管理器
    /// 统一管理游戏流程、玩家数据、配置等
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("游戏对象引用")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private ResourceSpawner resourceSpawner;
        [SerializeField] private EnemySpawner enemySpawner;

        [Header("游戏状态")]
        [SerializeField] private bool enableDebugLog = true;

        // 玩家数据和系统
        public PlayerData PlayerData { get; private set; }
        private PlayerStatsSystem _playerStatsSystem;

        // 游戏状态
        public bool IsGameRunning { get; private set; }

        // 配置缓存
        private Dictionary<int, ResourceConfig> _resourceConfigDict;
        private Dictionary<int, EnemyConfig> _enemyConfigDict;

        private void Awake()
        {
            // 单例模式
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        /// <summary>
        /// 初始化游戏管理器
        /// </summary>
        private void Initialize()
        {
            // 初始化玩家数据
            PlayerData = new PlayerData();
            _playerStatsSystem = new PlayerStatsSystem(PlayerData);

            // 加载配置
            LoadConfigs();

            // 监听玩家死亡
            PlayerData.HealthObserver.OnChange.Add(OnHealthChanged, this);

            LogDebug("游戏管理器初始化完成");
        }

        /// <summary>
        /// 加载配置表
        /// </summary>
        private void LoadConfigs()
        {
            // 初始化字典（即使配置加载失败也要初始化）
            _resourceConfigDict = new Dictionary<int, ResourceConfig>();
            _enemyConfigDict = new Dictionary<int, EnemyConfig>();

            try
            {
                // 尝试加载资源配置
                // 注意：如果用户还没有导出Excel，这里会失败，但游戏可以继续运行
                var resourceConfigs = GridFramework.Config.Get<Generate.Scripts.Configs.ResourceConfigs>();
                
                if (resourceConfigs?.Value != null)
                {
                    foreach (var config in resourceConfigs.Value)
                    {
                        var resourceStruct = new ResourceConfig
                        {
                            ID = config.ID,
                            Name = config.Name,
                            Health = config.Health,
                            CollectTime = config.CollectTime,
                            RewardType = config.RewardType,
                            RewardAmount = config.RewardAmount
                        };
                        _resourceConfigDict[config.ID] = resourceStruct;
                    }
                    LogDebug($"加载资源配置成功，共 {_resourceConfigDict.Count} 个");
                }
                else
                {
                    Debug.LogWarning("资源配置为空，请先创建Excel配置表并导出");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"加载资源配置失败：{e.Message}\n请通过 Tools/Excel导出 生成配置");
            }

            try
            {
                // 尝试加载敌人配置
                var enemyConfigs = GridFramework.Config.Get<Generate.Scripts.Configs.EnemyConfigs>();
                
                if (enemyConfigs?.Value != null)
                {
                    foreach (var config in enemyConfigs.Value)
                    {
                        var enemyStruct = new EnemyConfig
                        {
                            ID = config.ID,
                            Name = config.Name,
                            MaxHealth = config.MaxHealth,
                            Speed = config.Speed,
                            Damage = config.Damage,
                            AttackInterval = config.AttackInterval
                        };
                        _enemyConfigDict[config.ID] = enemyStruct;
                    }
                    LogDebug($"加载敌人配置成功，共 {_enemyConfigDict.Count} 个");
                }
                else
                {
                    Debug.LogWarning("敌人配置为空，请先创建Excel配置表并导出");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"加载敌人配置失败：{e.Message}\n请通过 Tools/Excel导出 生成配置");
            }
        }

        /// <summary>
        /// 生命值变化回调
        /// </summary>
        private void OnHealthChanged(float newValue, float oldValue)
        {
            if (newValue <= 0 && IsGameRunning)
            {
                OnPlayerDeath();
            }
        }

        #region 游戏流程控制

        /// <summary>
        /// 开始新游戏
        /// </summary>
        public void StartNewGame()
        {
            LogDebug("开始新游戏");

            // 重置玩家数据
            PlayerData.ResetData();
            PlayerData.ClearSaveData();

            // 启动游戏
            StartGameplay();
        }

        /// <summary>
        /// 继续游戏
        /// </summary>
        public void ContinueGame()
        {
            LogDebug("继续游戏");

            // 数据已经从存档加载
            StartGameplay();
        }

        /// <summary>
        /// 启动游戏
        /// </summary>
        private void StartGameplay()
        {
            IsGameRunning = true;

            // 启动玩家属性系统
            _playerStatsSystem.Start();

            // 启动敌人生成器
            if (enemySpawner != null)
            {
                enemySpawner.StartSpawner();
            }

            // 显示游戏HUD
            GridFramework.UI.Show<GameHUD>();

            LogDebug("游戏已启动");
        }

        /// <summary>
        /// 暂停游戏
        /// </summary>
        public void PauseGame()
        {
            if (!IsGameRunning) return;

            Time.timeScale = 0f;
            _playerStatsSystem.Pause();

            LogDebug("游戏已暂停");
        }

        /// <summary>
        /// 恢复游戏
        /// </summary>
        public void ResumeGame()
        {
            if (!IsGameRunning) return;

            Time.timeScale = 1f;
            _playerStatsSystem.Resume();

            LogDebug("游戏已恢复");
        }

        /// <summary>
        /// 玩家死亡
        /// </summary>
        private void OnPlayerDeath()
        {
            LogDebug("玩家死亡");

            IsGameRunning = false;

            // 停止属性系统
            _playerStatsSystem.Stop();

            // 停止敌人生成
            if (enemySpawner != null)
            {
                enemySpawner.StopSpawner();
            }

            // 显示游戏结束界面
            GridFramework.UI.Show<GameOverUI>();
        }

        /// <summary>
        /// 重新开始游戏
        /// </summary>
        public void RestartGame()
        {
            LogDebug("重新开始游戏");

            // 清理场景
            CleanupScene();

            // 重置数据并启动
            StartNewGame();
        }

        /// <summary>
        /// 返回主菜单
        /// </summary>
        public void ReturnToMainMenu()
        {
            LogDebug("返回主菜单");

            // 停止游戏
            IsGameRunning = false;
            _playerStatsSystem.Stop();

            if (enemySpawner != null)
            {
                enemySpawner.StopSpawner();
            }

            // 清理场景
            CleanupScene();

            // 隐藏所有游戏UI
            GridFramework.UI.Hide<GameHUD>();
            GridFramework.UI.Hide<GameOverUI>();

            // 显示主菜单
            GridFramework.UI.Show<MainMenuUI>();
        }

        /// <summary>
        /// 清理场景
        /// </summary>
        private void CleanupScene()
        {
            // 清理敌人
            if (enemySpawner != null)
            {
                enemySpawner.ClearAllEnemies();
            }

            // 恢复时间缩放
            Time.timeScale = 1f;

            LogDebug("场景已清理");
        }

        #endregion

        #region 配置访问

        /// <summary>
        /// 获取资源配置
        /// </summary>
        public ResourceConfig GetResourceConfig(int id)
        {
            if (_resourceConfigDict != null && _resourceConfigDict.TryGetValue(id, out var config))
            {
                return config;
            }

            Debug.LogWarning($"找不到资源配置：ID={id}");
            return default;
        }

        /// <summary>
        /// 获取敌人配置
        /// </summary>
        public EnemyConfig GetEnemyConfig(int id)
        {
            if (_enemyConfigDict != null && _enemyConfigDict.TryGetValue(id, out var config))
            {
                return config;
            }

            Debug.LogWarning($"找不到敌人配置：ID={id}");
            return default;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 记录日志
        /// </summary>
        private void LogDebug(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[GameManager] {message}");
            }
        }

        #endregion

        private void OnApplicationQuit()
        {
            // 游戏退出时停止系统
            _playerStatsSystem?.Stop();
        }
    }
}
