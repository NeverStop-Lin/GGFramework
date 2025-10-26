using UnityEngine;
using UnityEngine.UI;
using Framework.Core;
using Game.Manager;

namespace Game.UI
{
    /// <summary>
    /// 主菜单界面
    /// 提供开始游戏、继续游戏、退出游戏功能
    /// </summary>
    public partial class MainMenuUI
    {
        [Header("按钮")]
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button continueGameButton;
        [SerializeField] private Button exitGameButton;

        private GameManager _gameManager;

        #region 生命周期

        protected override void Awake()
        {
            base.Awake();

            // 绑定按钮事件
            if (startGameButton != null)
            {
                startGameButton.onClick.AddListener(OnStartGameClick);
            }

            if (continueGameButton != null)
            {
                continueGameButton.onClick.AddListener(OnContinueGameClick);
            }

            if (exitGameButton != null)
            {
                exitGameButton.onClick.AddListener(OnExitGameClick);
            }
        }

        protected override void OnCreate(params object[] args)
        {
        }

        protected override void OnShow(params object[] args)
        {
            _gameManager = GameManager.Instance;
            UpdateButtonStates();
        }

        protected override void OnHide(params object[] args)
        {
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // 解绑按钮事件
            if (startGameButton != null)
            {
                startGameButton.onClick.RemoveListener(OnStartGameClick);
            }

            if (continueGameButton != null)
            {
                continueGameButton.onClick.RemoveListener(OnContinueGameClick);
            }

            if (exitGameButton != null)
            {
                exitGameButton.onClick.RemoveListener(OnExitGameClick);
            }
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 点击开始游戏（新游戏）
        /// </summary>
        private void OnStartGameClick()
        {
            Debug.Log("点击开始游戏");

            if (_gameManager != null)
            {
                Hide();
                _gameManager.StartNewGame();
            }
        }

        /// <summary>
        /// 点击继续游戏
        /// </summary>
        private void OnContinueGameClick()
        {
            Debug.Log("点击继续游戏");

            if (_gameManager != null)
            {
                Hide();
                _gameManager.ContinueGame();
            }
        }

        /// <summary>
        /// 点击退出游戏
        /// </summary>
        private void OnExitGameClick()
        {
            Debug.Log("点击退出游戏");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// 更新按钮状态（根据是否有存档）
        /// </summary>
        private void UpdateButtonStates()
        {
            if (_gameManager == null) return;

            bool hasSaveData = _gameManager.PlayerData.HasSaveData();

            if (continueGameButton != null)
            {
                continueGameButton.interactable = hasSaveData;
            }
        }

        #endregion
    }
}
