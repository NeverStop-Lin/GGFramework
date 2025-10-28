using Framework.Core;
using Game.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 游戏结束界面
    /// 显示生存时间和提供重新开始选项
    /// </summary>
    public class GameOverUI : UIBehaviour
    {
        [Header("显示组件")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text survivalTimeText;
        [SerializeField] private Text collectStatsText;

        [Header("按钮")]
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;


        protected override void OnShow(params object[] args)
        {
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                DisplayGameResult(gameManager);
            }

            // 暂停游戏
            Time.timeScale = 0f;
        }

        protected override void OnHide(params object[] args)
        {
            // 恢复游戏时间
            Time.timeScale = 1f;
        }

        /// <summary>
        /// 显示游戏结果
        /// </summary>
        private void DisplayGameResult(GameManager manager)
        {
            var playerData = manager.PlayerData;

            // 标题
            if (titleText != null)
            {
                titleText.text = "游戏结束";
            }

            // 生存时间
            if (survivalTimeText != null)
            {
                float totalSeconds = playerData.SurvivalTime;
                int minutes = Mathf.FloorToInt(totalSeconds / 60f);
                int seconds = Mathf.FloorToInt(totalSeconds % 60f);
                survivalTimeText.text = $"生存时间: {minutes:00}:{seconds:00}";
            }

            // 收集统计
            if (collectStatsText != null)
            {
                collectStatsText.text = $"收集木材: {playerData.WoodCount}\n收集石头: {playerData.StoneCount}";
            }
        }

        /// <summary>
        /// 点击重新开始按钮
        /// </summary>
        private void OnRestartClick()
        {
            Debug.Log("点击重新开始");

            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                Hide();
                gameManager.RestartGame();
            }
        }

        /// <summary>
        /// 点击返回主菜单按钮
        /// </summary>
        private void OnMainMenuClick()
        {
            Debug.Log("点击返回主菜单");

            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                Hide();

                gameManager.ReturnToMainMenu();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // 解绑按钮事件
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(OnRestartClick);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveListener(OnMainMenuClick);
            }
        }
    }
}
