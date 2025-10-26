using Framework.Core;
using Framework.Scripts;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// 玩家数据管理
    /// 使用Observer模式管理玩家属性，支持数据持久化
    /// </summary>
    public class PlayerData
    {
        // 属性观察者
        private IValueObserver<float> _health;
        private IValueObserver<float> _hunger;
        private IValueObserver<float> _thirst;
        private IValueObserver<int> _woodCount;
        private IValueObserver<int> _stoneCount;
        private IValueObserver<float> _survivalTime;

        // 属性最大值
        public const float MaxHealth = 100f;
        public const float MaxHunger = 100f;
        public const float MaxThirst = 100f;

        public PlayerData()
        {
            InitializeObservers();
        }

        /// <summary>
        /// 初始化所有属性观察者
        /// </summary>
        private void InitializeObservers()
        {
            // 使用Cache类型的Observer，支持本地存储
            _health = GridFramework.Observer.Cache("Player_Health", MaxHealth);
            _hunger = GridFramework.Observer.Cache("Player_Hunger", MaxHunger);
            _thirst = GridFramework.Observer.Cache("Player_Thirst", MaxThirst);
            _woodCount = GridFramework.Observer.Cache("Player_WoodCount", 0);
            _stoneCount = GridFramework.Observer.Cache("Player_StoneCount", 0);
            _survivalTime = GridFramework.Observer.Cache("Player_SurvivalTime", 0f);
        }

        #region 属性访问器

        public float Health
        {
            get => _health.Value;
            set => _health.Value = Mathf.Clamp(value, 0, MaxHealth);
        }

        public float Hunger
        {
            get => _hunger.Value;
            set => _hunger.Value = Mathf.Clamp(value, 0, MaxHunger);
        }

        public float Thirst
        {
            get => _thirst.Value;
            set => _thirst.Value = Mathf.Clamp(value, 0, MaxThirst);
        }

        public int WoodCount
        {
            get => _woodCount.Value;
            set => _woodCount.Value = Mathf.Max(0, value);
        }

        public int StoneCount
        {
            get => _stoneCount.Value;
            set => _stoneCount.Value = Mathf.Max(0, value);
        }

        public float SurvivalTime
        {
            get => _survivalTime.Value;
            set => _survivalTime.Value = Mathf.Max(0, value);
        }

        #endregion

        #region 观察者访问

        public IValueObserver<float> HealthObserver => _health;
        public IValueObserver<float> HungerObserver => _hunger;
        public IValueObserver<float> ThirstObserver => _thirst;
        public IValueObserver<int> WoodCountObserver => _woodCount;
        public IValueObserver<int> StoneCountObserver => _stoneCount;
        public IValueObserver<float> SurvivalTimeObserver => _survivalTime;

        #endregion

        #region 数据操作

        /// <summary>
        /// 添加资源
        /// </summary>
        public void AddResource(string resourceType, int amount)
        {
            switch (resourceType)
            {
                case "Wood":
                    WoodCount += amount;
                    Debug.Log($"获得木材 x{amount}，当前数量：{WoodCount}");
                    break;
                case "Stone":
                    StoneCount += amount;
                    Debug.Log($"获得石头 x{amount}，当前数量：{StoneCount}");
                    break;
                default:
                    Debug.LogWarning($"未知的资源类型：{resourceType}");
                    break;
            }
        }

        /// <summary>
        /// 受到伤害
        /// </summary>
        public void TakeDamage(float damage)
        {
            Health -= damage;
            Debug.Log($"受到伤害 {damage}，当前生命值：{Health}");
        }

        /// <summary>
        /// 恢复生命
        /// </summary>
        public void RestoreHealth(float amount)
        {
            Health += amount;
        }

        /// <summary>
        /// 重置为新游戏状态
        /// </summary>
        public void ResetData()
        {
            Health = MaxHealth;
            Hunger = MaxHunger;
            Thirst = MaxThirst;
            WoodCount = 0;
            StoneCount = 0;
            SurvivalTime = 0f;
            
            Debug.Log("玩家数据已重置");
        }

        /// <summary>
        /// 检查是否存在存档
        /// </summary>
        public bool HasSaveData()
        {
            return GridFramework.Storage.HasKey("Player_Health");
        }

        /// <summary>
        /// 清除存档
        /// </summary>
        public void ClearSaveData()
        {
            GridFramework.Storage.Save("Player_Health", MaxHealth);
            GridFramework.Storage.Save("Player_Hunger", MaxHunger);
            GridFramework.Storage.Save("Player_Thirst", MaxThirst);
            GridFramework.Storage.Save("Player_WoodCount", 0);
            GridFramework.Storage.Save("Player_StoneCount", 0);
            GridFramework.Storage.Save("Player_SurvivalTime", 0f);
            
            Debug.Log("存档已清除");
        }

        #endregion
    }
}
