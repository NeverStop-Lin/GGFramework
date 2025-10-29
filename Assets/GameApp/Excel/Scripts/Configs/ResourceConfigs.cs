using System.Collections.Generic;
using Framework.Core;

namespace Generate.Scripts.Configs
{
	/// <summary>
	/// 资源配置
	/// </summary>
	public struct ResourceConfig
	{
		/// <summary> 资源ID </summary>
		public int ID;

		/// <summary> 资源名称 </summary>
		public string Name;

		/// <summary> 生命值 </summary>
		public int Health;

		/// <summary> 采集时间(秒) </summary>
		public float CollectTime;

		/// <summary> 奖励类型 </summary>
		public string RewardType;

		/// <summary> 奖励数量 </summary>
		public int RewardAmount;

	}

	public class ResourceConfigs : BaseConfig<List<ResourceConfig>>
	{
		public override string Url => $"Configs/Resource";
	}
}
