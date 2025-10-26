using Framework.Core;
using Framework.Scripts;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// 玩家属性系统
    /// 管理饥饿值和口渴值的持续消耗
    /// </summary>
    public class PlayerStatsSystem
    {
        private PlayerData _playerData;
        
        // 消耗速率（每秒）
        private const float HungerDecayRate = 2f;
        private const float ThirstDecayRate = 3f;
        
        // 低属性伤害
        private const float HungerDamage = 5f;
        private const float ThirstDamage = 8f;
        
        // 定时器实例
        private Timer _hungerTimer;
        private Timer _thirstTimer;
        private Timer _survivalTimer;
        
        private bool _isRunning;

        public PlayerStatsSystem(PlayerData playerData)
        {
            _playerData = playerData;
        }

        /// <summary>
        /// 启动属性系统
        /// </summary>
        public void Start()
        {
            if (_isRunning) return;
            
            _isRunning = true;
            
            // 饥饿值消耗定时器（每秒执行，无限次）
            _hungerTimer = GridFramework.Timer.Interval(() =>
            {
                _playerData.Hunger -= HungerDecayRate;
                
                // 饥饿值为0时持续扣血
                if (_playerData.Hunger <= 0)
                {
                    _playerData.TakeDamage(HungerDamage);
                }
            }, 1f, -1);
            
            // 口渴值消耗定时器（每秒执行，无限次）
            _thirstTimer = GridFramework.Timer.Interval(() =>
            {
                _playerData.Thirst -= ThirstDecayRate;
                
                // 口渴值为0时持续扣血
                if (_playerData.Thirst <= 0)
                {
                    _playerData.TakeDamage(ThirstDamage);
                }
            }, 1f, -1);
            
            // 生存时间计时器（每秒执行，无限次）
            _survivalTimer = GridFramework.Timer.Interval(() =>
            {
                _playerData.SurvivalTime += 1f;
            }, 1f, -1);
            
            Debug.Log("玩家属性系统已启动");
        }

        /// <summary>
        /// 停止属性系统
        /// </summary>
        public void Stop()
        {
            if (!_isRunning) return;
            
            _isRunning = false;
            
            _hungerTimer?.Stop();
            _thirstTimer?.Stop();
            _survivalTimer?.Stop();
            
            Debug.Log("玩家属性系统已停止");
        }

        /// <summary>
        /// 暂停属性系统
        /// </summary>
        public void Pause()
        {
            if (!_isRunning) return;
            
            _hungerTimer?.Pause();
            _thirstTimer?.Pause();
            _survivalTimer?.Pause();
        }

        /// <summary>
        /// 恢复属性系统
        /// </summary>
        public void Resume()
        {
            if (!_isRunning) return;
            
            _hungerTimer?.Resume();
            _thirstTimer?.Resume();
            _survivalTimer?.Resume();
        }
    }
}
