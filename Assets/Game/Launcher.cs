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
        public GameObject GameManagerPrefab;
        private GameManager _gameManagerInstance;

        protected override void BusinessInitialize()
        {
            // 在此添加业务初始化逻辑
            // GridFramework.UI.Show<MainMenuUI>();
            InitializeGame();
        }

        /// <summary>
        /// 初始化游戏
        /// </summary>
        private void InitializeGame()
        {
            Debug.Log("=== 生存游戏启动 ===");

            // 创建游戏管理器
            if (GameManager.Instance == null)
            {
                _gameManagerInstance = Instantiate(GameManagerPrefab).GetComponent<GameManager>();
            }

            // 显示主菜单
            GridFramework.UI.Show<MainMenuUI>();

            Debug.Log("游戏初始化完成");
        }


    }
}

