using System;
using System.Collections.Generic;
using KinematicCharacterController;
using UnityEngine;

namespace 游戏应用.角色
{
    /// <summary>
    /// 使用 KCC 的第三人称角色控制器，支持：移动、多段跳、飞扑、趴下、爬梯子、爬墙、登墙跳
    /// </summary>
    public class 角色控制器 : MonoBehaviour, ICharacterController
    {
        [Header("必须组件")]
        public KinematicCharacterMotor 运动器;
        public Transform 网格根节点;

        [Header("配置")]
        public 角色控制器配置 配置;

        [Header("运行时状态（只读）")]
        [SerializeField] private 角色状态 当前状态 = 角色状态.默认;
        [SerializeField] private int 已用跳跃次数 = 0;
        [SerializeField] private bool 本帧已跳跃 = false;
        [SerializeField] private Vector3 期望朝向 = Vector3.forward;
        [SerializeField] private Vector3 平面输入方向 = Vector3.zero;
        [SerializeField] private bool 正在蹲伏 = false;

        // 缓冲/宽限
        private float 自上次可跳起的时间 = 0f;
        private float 自请求跳跃的时间 = float.PositiveInfinity;
        private bool 已请求跳跃 = false;

        // 飞扑
        private bool 飞扑冷却中 = false;
        private float 飞扑剩余时间 = 0f;
        private Vector3 飞扑方向 = Vector3.zero;
        private float 飞扑冷却计时 = 0f;

        // 爬梯子
        private Collider 当前梯子;

        // 爬墙
        private Vector3 最近墙面法线 = Vector3.zero;
        private float 抓墙计时 = 0f;

        // 临时缓存
        private readonly Collider[] 探测碰撞体缓存 = new Collider[8];

        private void Reset()
        {
            运动器 = GetComponent<KinematicCharacterMotor>();
        }

        private void Awake()
        {
            if (运动器 == null)
            {
                throw new InvalidOperationException("KinematicCharacterMotor is required on the same GameObject");
            }
            if (配置 == null)
            {
                throw new InvalidOperationException("角色控制器配置 is missing");
            }

            运动器.CharacterController = this;
            切换状态(角色状态.默认);
        }

        public void 设置输入(ref 角色输入数据 输入)
        {
            // 组装平面移动方向（以角色上方向为参考）
            平面输入方向 = new Vector3(输入.平面移动输入.x, 0f, 输入.平面移动输入.y);
            if (平面输入方向.sqrMagnitude > 1f)
            {
                平面输入方向.Normalize();
            }

            // 朝向：默认取移动方向
            if (平面输入方向.sqrMagnitude > 0.0001f)
            {
                期望朝向 = 平面输入方向;
            }

            // 跳跃缓冲
            if (输入.跳跃按下)
            {
                已请求跳跃 = true;
                自请求跳跃的时间 = 0f;
            }

            // 飞扑请求
            if (输入.飞扑按下 && !飞扑冷却中 && 当前状态 != 角色状态.飞扑)
            {
                开始飞扑();
            }

            // 趴下切换
            if (输入.切换趴下按下)
            {
                切换趴下();
            }
        }

        private void 切换状态(角色状态 新状态)
        {
            角色状态 旧 = 当前状态;
            当前状态 = 新状态;

            // 进入/退出时机处理
            if (新状态 == 角色状态.飞扑)
            {
                飞扑剩余时间 = 配置.飞扑持续时间;
                飞扑冷却中 = true;
                飞扑冷却计时 = 0f;
            }
            else if (旧 == 角色状态.飞扑 && 新状态 != 角色状态.飞扑)
            {
                飞扑剩余时间 = 0f;
            }

            if (新状态 == 角色状态.爬墙)
            {
                抓墙计时 = 0f;
            }

            if (新状态 == 角色状态.爬梯子)
            {
                // 进入梯子时不受重力
            }
        }

        private void 切换趴下()
        {
            if (!正在蹲伏)
            {
                正在蹲伏 = true;
                运动器.SetCapsuleDimensions(配置.胶囊半径, 配置.蹲伏身高, 配置.蹲伏身高 * 0.5f);
                if (网格根节点 != null)
                {
                    var s = 网格根节点.localScale;
                    网格根节点.localScale = new Vector3(s.x, Mathf.Max(0.5f, 配置.蹲伏身高 / 配置.站立身高), s.z);
                }
            }
            else
            {
                // 站起：先检测空间
                运动器.SetCapsuleDimensions(配置.胶囊半径, 配置.站立身高, 配置.站立身高 * 0.5f);
                if (运动器.CharacterOverlap(
                        运动器.TransientPosition,
                        运动器.TransientRotation,
                        探测碰撞体缓存,
                        运动器.CollidableLayers,
                        QueryTriggerInteraction.Ignore) > 0)
                {
                    // 受阻，继续保持蹲伏
                    运动器.SetCapsuleDimensions(配置.胶囊半径, 配置.蹲伏身高, 配置.蹲伏身高 * 0.5f);
                    return;
                }

                正在蹲伏 = false;
                if (网格根节点 != null)
                {
                    var s = 网格根节点.localScale;
                    网格根节点.localScale = new Vector3(s.x, 1f, s.z);
                }
            }
        }

        private void 开始飞扑()
        {
            Vector3 冲向 = 平面输入方向.sqrMagnitude > 0.0001f ? 平面输入方向 : transform.forward;
            飞扑方向 = 冲向.normalized;
            切换状态(角色状态.飞扑);

            // 空中飞扑可选地消耗一次跳跃
            if (!运动器.GroundingStatus.IsStableOnGround && 配置.空中飞扑消耗一次跳跃 && 已用跳跃次数 < 配置.最大跳跃次数)
            {
                已用跳跃次数 = Mathf.Clamp(已用跳跃次数 + 1, 0, 配置.最大跳跃次数);
            }
        }

        public void BeforeCharacterUpdate(float deltaTime)
        {
        }

        public void UpdateRotation(ref Quaternion 当前旋转, float deltaTime)
        {
            Vector3 当前上 = 当前旋转 * Vector3.up;

            switch (当前状态)
            {
                case 角色状态.飞扑:
                    if (飞扑方向.sqrMagnitude > 0.0001f)
                    {
                        Vector3 目标朝向 = Vector3.Slerp(运动器.CharacterForward, 飞扑方向,
                            1 - Mathf.Exp(-配置.方向插值锐度 * deltaTime)).normalized;
                        当前旋转 = Quaternion.LookRotation(目标朝向, 当前上);
                    }
                    break;

                case 角色状态.爬墙:
                    if (最近墙面法线 != Vector3.zero)
                    {
                        Vector3 朝外 = Vector3.ProjectOnPlane(-最近墙面法线, 当前上).normalized;
                        if (朝外.sqrMagnitude > 0.0001f)
                        {
                            Vector3 目标朝向 = Vector3.Slerp(运动器.CharacterForward, 朝外,
                                1 - Mathf.Exp(-配置.方向插值锐度 * deltaTime)).normalized;
                            当前旋转 = Quaternion.LookRotation(目标朝向, 当前上);
                        }
                    }
                    break;

                default:
                    if (期望朝向.sqrMagnitude > 0.0001f)
                    {
                        Vector3 目标朝向 = Vector3.Slerp(运动器.CharacterForward, 期望朝向,
                            1 - Mathf.Exp(-配置.方向插值锐度 * deltaTime)).normalized;
                        当前旋转 = Quaternion.LookRotation(目标朝向, 当前上);
                    }
                    break;
            }

            // 始终将角色上方向对齐世界上（或配置重力方向的反向）
            Vector3 平滑重力上 = Vector3.Slerp(当前上, Vector3.up, 1 - Mathf.Exp(-配置.方向插值锐度 * deltaTime));
            当前旋转 = Quaternion.FromToRotation(当前上, 平滑重力上) * 当前旋转;
        }

        public void UpdateVelocity(ref Vector3 当前速度, float deltaTime)
        {
            // 冷却/计时更新
            if (飞扑冷却中)
            {
                飞扑冷却计时 += deltaTime;
                if (飞扑冷却计时 >= 配置.飞扑冷却时间)
                {
                    飞扑冷却中 = false;
                }
            }

            // 接地/空中状态维护
            bool 在稳定地面上 = 运动器.GroundingStatus.IsStableOnGround;
            本帧已跳跃 = false;
            自请求跳跃的时间 += deltaTime;
            if (在稳定地面上)
            {
                自上次可跳起的时间 = 0f;
                if (当前状态 == 角色状态.空中)
                {
                    切换状态(角色状态.默认);
                }
                已用跳跃次数 = 0; // 着地重置跳跃计数
            }
            else
            {
                自上次可跳起的时间 += deltaTime;
                if (当前状态 == 角色状态.默认)
                {
                    切换状态(角色状态.空中);
                }
            }

            switch (当前状态)
            {
                case 角色状态.飞扑:
                {
                    // 飞扑速度沿平面方向，并快速衰减
                    if (飞扑剩余时间 > 0f)
                    {
                        Vector3 目标水平 = 飞扑方向 * 配置.飞扑初速度;
                        Vector3 当前水平 = Vector3.ProjectOnPlane(当前速度, 运动器.CharacterUp);
                        当前水平 = Vector3.Lerp(当前水平, 目标水平, 1f - Mathf.Exp(-配置.飞扑衰减 * deltaTime));
                        当前速度 = 当前水平 + Vector3.Project(当前速度, 运动器.CharacterUp);
                        飞扑剩余时间 -= deltaTime;
                    }
                    else
                    {
                        // 飞扑结束，回到空中或地面
                        切换状态(在稳定地面上 ? 角色状态.默认 : 角色状态.空中);
                    }
                    // 飞扑不受重力影响太大，轻微施加
                    当前速度 += 配置.重力 * 0.25f * deltaTime;
                    break;
                }

                case 角色状态.爬梯子:
                {
                    // 忽略重力，只允许沿上方向移动
                    float 前进 = Mathf.Max(0f, Vector3.Dot(平面输入方向, transform.forward));
                    float 后退 = Mathf.Max(0f, Vector3.Dot(-平面输入方向, transform.forward));
                    float 垂直 = (前进 - 后退) * 配置.爬梯子速度;
                    当前速度 = transform.up * 垂直;

                    // 离开梯子条件：远离或无梯子，或跳跃
                    bool 附近无梯子 = !检测梯子(out _);
                    if (附近无梯子)
                    {
                        切换状态(在稳定地面上 ? 角色状态.默认 : 角色状态.空中);
                    }
                    break;
                }

                case 角色状态.爬墙:
                {
                    // 缓慢下滑，保持贴墙
                    抓墙计时 += deltaTime;
                    Vector3 垂直速度 = Vector3.Project(当前速度, 运动器.CharacterUp);
                    当前速度 = Vector3.ProjectOnPlane(当前速度, 运动器.CharacterUp);
                    当前速度 += -运动器.CharacterUp * 配置.下滑速度;
                    当前速度 += 垂直速度 * 0.0f; // 清除原有竖直上升

                    // 超时或离开墙体则掉落
                    if (抓墙计时 >= 配置.抓墙最久时间 || !可爬墙朝向保持(out _))
                    {
                        切换状态(角色状态.空中);
                    }
                    // 重力小量影响
                    当前速度 += 配置.重力 * 0.2f * deltaTime;
                    break;
                }

                case 角色状态.默认:
                case 角色状态.空中:
                default:
                {
                    if (在稳定地面上)
                    {
                        // 地面重新定向速度以顺着地面
                        float 现速大小 = 当前速度.magnitude;
                        Vector3 有效法线 = 运动器.GroundingStatus.GroundNormal;
                        当前速度 = 运动器.GetDirectionTangentToSurface(当前速度, 有效法线) * 现速大小;

                        // 目标速度
                        Vector3 输入右 = Vector3.Cross(平面输入方向, 运动器.CharacterUp);
                        Vector3 重新定向输入 = Vector3.Cross(有效法线, 输入右).normalized * 平面输入方向.magnitude;
                        float 速度上限 = 配置.最大稳定移动速度 * (正在蹲伏 ? 配置.蹲伏移动速度倍率 : 1f);
                        Vector3 目标速度 = 重新定向输入 * 速度上限;
                        当前速度 = Vector3.Lerp(当前速度, 目标速度, 1f - Mathf.Exp(-配置.稳定移动锐度 * deltaTime));
                    }
                    else
                    {
                        // 空中添加输入
                        if (平面输入方向.sqrMagnitude > 0f)
                        {
                            Vector3 添加 = 平面输入方向 * 配置.空中加速度 * deltaTime;
                            Vector3 现平面 = Vector3.ProjectOnPlane(当前速度, 运动器.CharacterUp);
                            if (现平面.magnitude < 配置.最大空中移动速度)
                            {
                                Vector3 限制后 = Vector3.ClampMagnitude(现平面 + 添加, 配置.最大空中移动速度);
                                添加 = 限制后 - 现平面;
                            }

                            // 贴坡面防止沿斜墙爬升
                            if (运动器.GroundingStatus.FoundAnyGround)
                            {
                                if (Vector3.Dot(当前速度 + 添加, 添加) > 0f)
                                {
                                    Vector3 垂直阻碍法线 = Vector3.Cross(Vector3.Cross(运动器.CharacterUp, 运动器.GroundingStatus.GroundNormal), 运动器.CharacterUp).normalized;
                                    添加 = Vector3.ProjectOnPlane(添加, 垂直阻碍法线);
                                }
                            }
                            当前速度 += 添加;
                        }

                        // 重力与阻力
                        当前速度 += 配置.重力 * deltaTime;
                        当前速度 *= (1f / (1f + (配置.空中阻力 * deltaTime)));
                    }

                    // 跳跃处理（缓冲 + 土狼时间 + 多段跳）
                    if (已请求跳跃)
                    {
                        bool 可跳 =
                            (在稳定地面上 || 自上次可跳起的时间 <= 配置.土狼时间) ||
                            (已用跳跃次数 < 配置.最大跳跃次数);

                        bool 缓冲有效 = 自请求跳跃的时间 <= 配置.跳跃缓冲时间;
                        if (可跳 && 缓冲有效)
                        {
                            Vector3 跳跃方向 = 运动器.CharacterUp;
                            if (运动器.GroundingStatus.FoundAnyGround && !运动器.GroundingStatus.IsStableOnGround)
                            {
                                跳跃方向 = 运动器.GroundingStatus.GroundNormal;
                            }

                            运动器.ForceUnground();
                            当前速度 += (跳跃方向 * 配置.跳起速度) - Vector3.Project(当前速度, 运动器.CharacterUp);
                            当前速度 += (平面输入方向 * 配置.跳跃前向速度);

                            已用跳跃次数 = (在稳定地面上 || 自上次可跳起的时间 <= 配置.土狼时间)
                                ? 1 // 起跳视为已使用一次
                                : Mathf.Clamp(已用跳跃次数 + 1, 1, 配置.最大跳跃次数);

                            本帧已跳跃 = true;
                            已请求跳跃 = false;
                            自请求跳跃的时间 = float.PositiveInfinity;

                            // 跳跃可打断飞扑与爬墙/爬梯子
                            if (当前状态 == 角色状态.飞扑 || 当前状态 == 角色状态.爬墙 || 当前状态 == 角色状态.爬梯子)
                            {
                                切换状态(角色状态.空中);
                            }
                        }
                        else if (!缓冲有效)
                        {
                            已请求跳跃 = false;
                        }
                    }

                    // 梯子吸附检测（按前进靠近梯子时进入）
                    if (当前状态 != 角色状态.爬梯子 && 检测梯子(out 当前梯子))
                    {
                        // 若玩家朝前输入，吸附
                        if (Vector3.Dot(平面输入方向, transform.forward) > 0.1f)
                        {
                            切换状态(角色状态.爬梯子);
                        }
                    }

                    break;
                }
            }
        }

        public void AfterCharacterUpdate(float deltaTime)
        {
            // 解除蹲伏：若当前是蹲伏且玩家未再切换，将在输入触发时处理，这里只维护可站起的空间检测由切换逻辑处理
        }

        public void PostGroundingUpdate(float deltaTime)
        {
            // 着地/离地事件留空，外部可扩展
        }

        public bool IsColliderValidForCollisions(Collider coll)
        {
            return true;
        }

        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }

        public void OnMovementHit(Collider 命中碰撞体, Vector3 命中法线, Vector3 命中点, ref HitStabilityReport 稳定报告)
        {
            // 空中时若正面撞到可爬墙表面，进入爬墙
            if (!运动器.GroundingStatus.IsStableOnGround && 可爬墙法线(命中法线))
            {
                最近墙面法线 = 命中法线;
                if (可爬墙朝向保持(out _))
                {
                    切换状态(角色状态.爬墙);
                }
            }
        }

        public void AddVelocity(Vector3 velocity)
        {
            // 提供外部推力接口：合并为瞬时速度增量
            运动器.BaseVelocity += velocity;
        }

        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
        }

        public void OnDiscreteCollisionDetected(Collider hitCollider)
        {
        }

        private bool 可爬墙法线(Vector3 法线)
        {
            // 法线与上方向的夹角大于阈值（接近垂直墙）
            float 角 = Vector3.Angle(法线, Vector3.up);
            return 角 >= 配置.可爬墙最大表面角度 * 0.5f; // 宽松一些
        }

        private bool 可爬墙朝向保持(out RaycastHit 命中)
        {
            Vector3 起点 = 运动器.TransientPosition + 运动器.CharacterUp * 0.5f;
            bool 有命中 = Physics.Raycast(起点, -最近墙面法线.normalized, out 命中, 配置.贴墙检测距离, 配置.可爬墙层, QueryTriggerInteraction.Ignore);
            return 有命中;
        }

        private bool 检测梯子(out Collider 梯子)
        {
            梯子 = null;
            Vector3 原点 = 运动器.TransientPosition + 运动器.CharacterUp * (配置.胶囊半径 + 0.1f);
            Vector3 方向 = transform.forward;
            int 命中数 = Physics.OverlapSphereNonAlloc(原点 + 方向 * 配置.梯子检测距离, 配置.梯子检测半径, 探测碰撞体缓存, 配置.梯子层, QueryTriggerInteraction.Collide);
            for (int i = 0; i < 命中数; i++)
            {
                Collider c = 探测碰撞体缓存[i];
                if (c == null) continue;
                if (string.IsNullOrEmpty(配置.梯子Tag) || c.CompareTag(配置.梯子Tag))
                {
                    梯子 = c;
                    return true;
                }
            }
            return false;
        }
    }
}



