using Framework.Core;
using Game.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 游戏主界面HUD
    /// 显示玩家状态和资源信息
    /// </summary>
    public  class GameHUD : UIBehaviour
    {
        [Header("状态条")]
        [SerializeField] private Slider healthBar;
        [SerializeField] private Slider hungerBar;
        [SerializeField] private Slider thirstBar;

        [Header("文本显示")]
        [SerializeField] private Text healthText;
        [SerializeField] private Text hungerText;
        [SerializeField] private Text thirstText;
        [SerializeField] private Text woodCountText;
        [SerializeField] private Text stoneCountText;
        [SerializeField] private Text survivalTimeText;

        [Header("提示信息")]
        [SerializeField] private Text interactHintText;

        private GameManager _gameManager;

        protected override void OnShow(params object[] args)
        {
            base.OnShow(args);

            _gameManager = GameManager.Instance;
            if (_gameManager != null)
            {
                BindDataObservers();
                InitializeDisplay();
            }
        }

        protected override void OnHide(params object[] args)
        {
            base.OnHide(args);
            UnbindDataObservers();
        }

        /// <summary>
        /// 绑定数据观察者
        /// </summary>
        private void BindDataObservers()
        {
            var playerData = _gameManager.PlayerData;

            // 生命值变化
            playerData.HealthObserver.OnChange.Add((newVal, oldVal) => UpdateHealthDisplay(newVal, oldVal), this);

            // 饥饿值变化
            playerData.HungerObserver.OnChange.Add((newVal, oldVal) => UpdateHungerDisplay(newVal, oldVal), this);

            // 口渴值变化
            playerData.ThirstObserver.OnChange.Add((newVal, oldVal) => UpdateThirstDisplay(newVal, oldVal), this);

            // 木材数量变化
            playerData.WoodCountObserver.OnChange.Add((newVal, oldVal) => UpdateWoodDisplay(newVal, oldVal), this);

            // 石头数量变化
            playerData.StoneCountObserver.OnChange.Add((newVal, oldVal) => UpdateStoneDisplay(newVal, oldVal), this);

            // 生存时间变化
            playerData.SurvivalTimeObserver.OnChange.Add((newVal, oldVal) => UpdateSurvivalTimeDisplay(newVal, oldVal), this);
        }

        /// <summary>
        /// 解绑数据观察者
        /// </summary>
        private void UnbindDataObservers()
        {
            if (_gameManager == null) return;

            var playerData = _gameManager.PlayerData;

            // 注意：由于使用了lambda表达式，无法直接Remove
            // 这里可以选择不解绑，或者改用保存委托引用的方式
            // 暂时注释掉，因为UI销毁时数据也会清理
        }

        /// <summary>
        /// 初始化显示
        /// </summary>
        private void InitializeDisplay()
        {
            var playerData = _gameManager.PlayerData;

            UpdateHealthDisplay(playerData.Health, 0);
            UpdateHungerDisplay(playerData.Hunger, 0);
            UpdateThirstDisplay(playerData.Thirst, 0);
            UpdateWoodDisplay(playerData.WoodCount, 0);
            UpdateStoneDisplay(playerData.StoneCount, 0);
            UpdateSurvivalTimeDisplay(playerData.SurvivalTime, 0);

            if (interactHintText != null)
            {
                interactHintText.text = "按 E 采集资源";
            }
        }

        #region 更新显示方法

        private void UpdateHealthDisplay(float newValue, float oldValue)
        {
            if (healthBar != null)
            {
                healthBar.value = newValue / Player.PlayerData.MaxHealth;
            }

            if (healthText != null)
            {
                healthText.text = $"{newValue:F0}/{Player.PlayerData.MaxHealth:F0}";
            }
        }

        private void UpdateHungerDisplay(float newValue, float oldValue)
        {
            if (hungerBar != null)
            {
                hungerBar.value = newValue / Player.PlayerData.MaxHunger;
            }

            if (hungerText != null)
            {
                hungerText.text = $"{newValue:F0}/{Player.PlayerData.MaxHunger:F0}";
            }
        }

        private void UpdateThirstDisplay(float newValue, float oldValue)
        {
            if (thirstBar != null)
            {
                thirstBar.value = newValue / Player.PlayerData.MaxThirst;
            }

            if (thirstText != null)
            {
                thirstText.text = $"{newValue:F0}/{Player.PlayerData.MaxThirst:F0}";
            }
        }

        private void UpdateWoodDisplay(int newValue, int oldValue)
        {
            if (woodCountText != null)
            {
                woodCountText.text = $"木材: {newValue}";
            }
        }

        private void UpdateStoneDisplay(int newValue, int oldValue)
        {
            if (stoneCountText != null)
            {
                stoneCountText.text = $"石头: {newValue}";
            }
        }

        private void UpdateSurvivalTimeDisplay(float newValue, float oldValue)
        {
            if (survivalTimeText != null)
            {
                int minutes = Mathf.FloorToInt(newValue / 60f);
                int seconds = Mathf.FloorToInt(newValue % 60f);
                survivalTimeText.text = $"生存时间: {minutes:00}:{seconds:00}";
            }
        }

        #endregion
    }
}
