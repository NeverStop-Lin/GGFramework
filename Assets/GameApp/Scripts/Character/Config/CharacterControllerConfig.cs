using UnityEngine;
using Framework.Core.Attributes;

namespace GameApp.Character
{
    /// <summary>
    /// 角色控制器参数配置（ScriptableObject）
    /// </summary>
    [CreateAssetMenu(fileName = "DefaultCharacterConfig", menuName = "GameApp/Character/CharacterControllerConfig")]
    public class CharacterControllerConfig : ScriptableObject
    {
        [Header("基础移动")]
        [Label("最大稳定移动速度")]
        public float MaxStableMoveSpeed = 10f;
        [Label("地面移动平滑度", "数值越大加速/减速越快")]
        public float StableMovementSmoothness = 80f;
        [Label("地面摩擦力", "无输入时的速度衰减，越大停止越快")]
        public float GroundFriction = 80f;
        [Label("转向平滑度", "数值越大转向越快")]
        public float RotationSmoothness = 10f;

        [Header("空中移动")]
        [Label("最大空中移动速度")]
        public float MaxAirMoveSpeed = 10f;
        [Label("空中加速度")]
        public float AirAcceleration = 50f;
        [Label("空中阻力", "无输入时的水平速度衰减，越大停止越快")]
        public float AirDrag = 4f;
        [Label("重力")]
        public Vector3 Gravity = new Vector3(0, -60f, 0);

        [Header("跳跃")]
        [Label("跳跃持续时间", "跳跃力施加的时间")]
        public float JumpDuration = 0.4f;
        [Label("跳跃力度", "跳跃时的最大垂直速度（米/秒），配合曲线控制跳跃轨迹")]
        public float JumpForce = 10f;
        [Label("跳跃速度曲线", "X=时间进度0-1，Y=速度倍率0-1")]
        public AnimationCurve JumpSpeedCurve;
        [Label("最大跳跃次数", "支持多段跳，1=单跳，2=二段跳")]
        public int MaxJumpCount = 2;
        [Label("跳跃缓冲时间", "提前按下跳跃的缓冲时间")]
        public float JumpBufferTime = 0.15f;

        [Header("飞扑")]
        [Label("飞扑速度系数", "飞扑速度 = 系数 × 当前输入速度")]
        public float DashSpeedMultiplier = 1.5f;
        [Label("飞扑最小速度", "飞扑的最小速度限制")]
        public float DashMinSpeed = 8f;
        [Label("飞扑最大速度", "飞扑的最大速度限制")]
        public float DashMaxSpeed = 20f;
        [Label("地面滑行减速平滑度", "飞扑落地后减速的平滑程度，数值越大减速越快")]
        public float DashGroundSmoothness = 3f;
        [Label("飞扑转向平滑度", "飞扑时的转向平滑程度，数值越大转向越快")]
        public float DashRotationSmoothness = 5f;
    }
}



