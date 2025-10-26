using System.Collections.Generic;
using Framework.Core;

namespace Generate.Scripts.Configs
{
	/// <summary>
	/// 敌人配置
	/// </summary>
	public struct EnemyConfig
	{
		/// <summary> 敌人ID </summary>
		public int ID;

		/// <summary> 敌人名称 </summary>
		public string Name;

		/// <summary> 最大生命值 </summary>
		public int MaxHealth;

		/// <summary> 移动速度 </summary>
		public float Speed;

		/// <summary> 攻击伤害 </summary>
		public int Damage;

		/// <summary> 攻击间隔(秒) </summary>
		public float AttackInterval;

	}

	public class EnemyConfigs : BaseConfig<List<EnemyConfig>>
	{
		public override string Url => $"Configs/Enemy";
	}
}
