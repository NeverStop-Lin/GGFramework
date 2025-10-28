using UnityEngine;
using UnityEngine.UI;
using Framework.Core;
using Game.Manager;
using Framework.Scripts;
using System;

namespace Game.UI
{
    /// <summary>
    /// 主菜单界面
    /// 提供开始游戏、继续游戏、退出游戏功能
    /// </summary>
    public partial class MainMenuUI : UIBehaviour
    {
        [Header("按钮")]

        public Button _startGameButton;
        public Button _continueGameButton;
        public Button _exitGameButton;



        private GameManager _gameManager;


        protected override void OnCreate(params object[] args)
        {
            _startGameButton.onClick.AddListener(OnStartGameClick);
            _continueGameButton.onClick.AddListener(OnContinueGameClick);
            _exitGameButton.onClick.AddListener(OnExitGameClick);
        }
        protected override void OnRemove(params object[] args)
        {
            _startGameButton.onClick.RemoveListener(OnStartGameClick);
            _continueGameButton.onClick.RemoveListener(OnContinueGameClick);
            _exitGameButton.onClick.RemoveListener(OnExitGameClick);
        }


        protected override void OnShow(params object[] args)
        {
            _gameManager = GameManager.Instance;
            UpdateButtonStates();
        }

        protected override object OnHide(params object[] args)
        {
            return null;
        }




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
        private async void OnContinueGameClick()
        {
            Debug.Log("点击继续游戏");
            
            // 显示确认对话框
            var lifecycle = GridFramework.UI.Show<UI_002>();
            // 等待用户操作并获取结果
            var result = await lifecycle.HideTask;
            
            // 处理结果
            if (result is bool confirmed && confirmed)
            {
                Debug.Log("用户确认继续游戏");
                if (_gameManager != null)
                {
                    await Hide();
                    _gameManager.ContinueGame();
                }
            }
            else
            {
                Debug.Log("用户取消了操作");
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
            if (_continueGameButton != null)
            {
                _continueGameButton.interactable = hasSaveData;
            }
        }

        #endregion


        private void OnContinueClick()
        {
        }



    }
}
