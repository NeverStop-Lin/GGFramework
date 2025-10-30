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
        [Label("稳定移动锐度")]
        public float StableMovementSharpness = 15f;
        [Label("方向插值锐度")]
        public float OrientationSharpness = 10f;

        [Header("空中移动")]
        [Label("最大空中移动速度")]
        public float MaxAirMoveSpeed = 15f;
        [Label("空中加速度")]
        public float AirAcceleration = 15f;
        [Label("空中阻力")]
        public float AirDrag = 0.1f;
        [Label("重力")]
        public Vector3 Gravity = new Vector3(0, -30f, 0);

        [Header("跳跃")]
        [Label("斜坡可跳")]
        public bool AllowJumpingWhenSliding = false;
        [Label("跳起速度", "影响跳跃高度")]
        public float JumpUpSpeed = 10f;
        [Label("跳跃前向速度", "跳跃时的水平推进力")]
        public float JumpForwardSpeed = 10f;
        [Label("最大跳跃次数", "支持多段跳，1=单跳，2=二段跳",false)]
        public int MaxJumpCount = 2;
        [Label("土狼时间", "离地后仍可跳的宽限时间")]
        public float CoyoteTime = 0.1f;
        [Label("跳跃缓冲时间", "提前按下跳跃的缓冲时间")]
        public float JumpBufferTime = 0.1f;

        [Header("飞扑")]
        [Label("飞扑初速度")]
        public float DashSpeed = 12f;
        [Label("飞扑持续时间")]
        public float DashDuration = 0.25f;
        [Label("飞扑衰减")]
        public float DashDecay = 12f;
        [Label("飞扑冷却时间")]
        public float DashCooldown = 0.5f;
        [Label("空中飞扑消耗一次跳跃")]
        public bool AirDashConsumesJump = true;

        [Header("趴下/蹲伏")]
        [Label("蹲伏身高")]
        public float CrouchedHeight = 1.0f;
        [Label("站立身高")]
        public float StandingHeight = 2.0f;
        [Label("胶囊半径")]
        public float CapsuleRadius = 0.5f;
        [Label("蹲伏移动速度倍率")]
        public float CrouchedSpeedMultiplier = 0.5f;

        [Header("爬梯子")]
        [Label("梯子检测半径")]
        public float LadderDetectionRadius = 0.4f;
        [Label("梯子检测距离")]
        public float LadderDetectionDistance = 0.8f;
        [Label("爬梯子速度")]
        public float LadderClimbSpeed = 4f;
        [Label("梯子Tag")]
        public string LadderTag = "Ladder";
        [Label("梯子层")]
        public LayerMask LadderLayer;

        [Header("爬墙/登墙跳")]
        [Label("可爬墙层")]
        public LayerMask ClimbableWallLayer;
        [Label("可爬墙最大表面角度")]
        public float MaxWallClimbAngle = 75f;
        [Label("贴墙检测距离")]
        public float WallStickDistance = 0.6f;
        [Label("下滑速度")]
        public float WallSlideSpeed = 2.5f;
        [Label("抓墙最久时间")]
        public float MaxWallGrabDuration = 3.0f;
        [Label("登墙跳上升速度")]
        public float WallJumpUpSpeed = 10f;
        [Label("登墙跳后退速度")]
        public float WallJumpBackSpeed = 8f;
    }
}



