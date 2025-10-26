using Framework.Scripts;
using Game.Manager;
using Game.UI;
using UnityEngine;

namespace Framework.Scripts
{
    /// <summary>
    /// 游戏启动器
    /// 负责初始化游戏管理器和显示主菜单
    /// </summary>
    public partial class Launcher
    {
        [Header("游戏管理器")]
        [SerializeField] private GameManager gameManagerPrefab;

        private GameManager _gameManagerInstance;

        void Start()
        {
            InitializeGame();
        }

        /// <summary>
        /// 初始化游戏
        /// </summary>
        private void InitializeGame()
        {
            Debug.Log("=== 生存游戏启动 ===");

            // 创建游戏管理器
            if (gameManagerPrefab != null)
            {
                _gameManagerInstance = Instantiate(gameManagerPrefab);
            }
            else if (GameManager.Instance == null)
            {
                // 如果没有预制体，动态创建
                var managerObject = new GameObject("GameManager");
                _gameManagerInstance = managerObject.AddComponent<GameManager>();
            }

            // 显示主菜单
            GridFramework.UI.Show<MainMenuUI>();

            Debug.Log("游戏初始化完成");
        }
    }
}

