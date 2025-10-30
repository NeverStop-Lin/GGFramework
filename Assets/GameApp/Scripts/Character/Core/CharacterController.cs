using System;
using System.Collections.Generic;
using KinematicCharacterController;
using UnityEngine;
using Framework.Core.Attributes;

namespace GameApp.Character
{
    /// <summary>
    /// 使用 KCC 的第三人称角色控制器，支持：移动、多段跳、飞扑、趴下、爬梯子、爬墙、登墙跳
    /// </summary>
    public class CharacterController : MonoBehaviour, ICharacterController
    {
        [Header("必须组件")]
        [Label("运动器")]
        public KinematicCharacterMotor Motor;
        [Label("网格根节点")]
        public Transform MeshRoot;

        [Header("配置")]
        [Label("角色控制器配置")]
        public CharacterControllerConfig Config;

        [Header("运行时状态（只读）")]
        [SerializeField] [Framework.Core.Attributes.ReadOnly] [Label("当前状态")] private CharacterState _currentState = CharacterState.Default;
        [SerializeField] [Framework.Core.Attributes.ReadOnly] [Label("已用跳跃次数")] private int _jumpConsumed = 0;
        [SerializeField] [Framework.Core.Attributes.ReadOnly] [Label("本帧已跳跃")] private bool _jumpedThisFrame = false;
        [SerializeField] [Framework.Core.Attributes.ReadOnly] [Label("期望朝向")] private Vector3 _desiredForward = Vector3.forward;
        [SerializeField] [Framework.Core.Attributes.ReadOnly] [Label("平面输入方向")] private Vector3 _moveInputVector = Vector3.zero;
        [SerializeField] [Framework.Core.Attributes.ReadOnly] [Label("正在蹲伏")] private bool _isCrouching = false;

        // 公共查询接口
        public CharacterState CurrentState => _currentState; // 当前状态
        public bool JumpedThisFrame => _jumpedThisFrame; // 本帧已跳跃
        public bool IsCrouching => _isCrouching; // 正在蹲伏

        // 缓冲/宽限
        private float _timeSinceLastAbleToJump = 0f; // 上次能跳跃的时间
        private float _timeSinceJumpRequested = float.PositiveInfinity; // 上次请求跳跃的时间
        private bool _jumpRequested = false; // 是否请求跳跃

        // 飞扑
        private bool _dashOnCooldown = false; // 是否在飞扑冷却中
        private float _dashTimeRemaining = 0f; // 飞扑剩余时间
        private Vector3 _dashDirection = Vector3.zero; // 飞扑方向
        private float _dashCooldownTimer = 0f; // 飞扑冷却计时器

        // 爬梯子
        private Collider _currentLadder; // 当前梯子

        // 爬墙
        private Vector3 _lastWallNormal = Vector3.zero; // 上次爬墙的法线
        private float _wallGrabTimer = 0f; // 爬墙计时器

        // 临时缓存
        private readonly Collider[] _probedColliders = new Collider[8]; // 临时缓存碰撞器

        // 是 Unity 的一个特殊生命周期方法，组件首次添加或者在 Inspector 窗口中点击 Reset 按钮时调用
        private void Reset()
        {
            Motor = GetComponent<KinematicCharacterMotor>();
        }

        private void Awake()
        {
            if (Motor == null)
            {
                throw new InvalidOperationException("KinematicCharacterMotor is required on the same GameObject");
            }
            if (Config == null)
            {
                throw new InvalidOperationException("CharacterControllerConfig is missing");
            }

            Motor.CharacterController = this;
            TransitionToState(CharacterState.Default);
        }

        // 设置输入
        public void SetInput(ref CharacterInputData input)
        {
            // 组装平面移动方向（以角色上方向为参考）
            _moveInputVector = new Vector3(input.MoveInput.x, 0f, input.MoveInput.y);
            if (_moveInputVector.sqrMagnitude > 1f)
            {
                _moveInputVector.Normalize();
            }

            // 朝向：默认取移动方向
            if (_moveInputVector.sqrMagnitude > 0.0001f)
            {
                _desiredForward = _moveInputVector;
            }

            // 跳跃缓冲
            if (input.JumpDown)
            {
                _jumpRequested = true;
                _timeSinceJumpRequested = 0f;
            }

            // 飞扑请求
            if (input.DashDown && !_dashOnCooldown && _currentState != CharacterState.Dash)
            {
                StartDash();
            }

            // 趴下切换
            if (input.ToggleCrouchDown)
            {
                ToggleCrouch();
            }
        }

        // 过渡到新状态
        private void TransitionToState(CharacterState newState)
        {
            CharacterState oldState = _currentState;
            _currentState = newState;

            // 进入/退出时机处理
            if (newState == CharacterState.Dash)
            {
                _dashTimeRemaining = Config.DashDuration;
                _dashOnCooldown = true;
                _dashCooldownTimer = 0f;
            }
            else if (oldState == CharacterState.Dash && newState != CharacterState.Dash)
            {
                _dashTimeRemaining = 0f;
            }

            if (newState == CharacterState.WallClimb)
            {
                _wallGrabTimer = 0f;
            }

            if (newState == CharacterState.Ladder)
            {
                // 进入梯子时不受重力
            }
        }

        // 切换蹲伏
        private void ToggleCrouch()
        {
            if (!_isCrouching)
            {
                _isCrouching = true;
                Motor.SetCapsuleDimensions(Config.CapsuleRadius, Config.CrouchedHeight, Config.CrouchedHeight * 0.5f);
                if (MeshRoot != null)
                {
                    var s = MeshRoot.localScale;
                    MeshRoot.localScale = new Vector3(s.x, Mathf.Max(0.5f, Config.CrouchedHeight / Config.StandingHeight), s.z);
                }
            }
            else
            {
                // 站起：先检测空间
                Motor.SetCapsuleDimensions(Config.CapsuleRadius, Config.StandingHeight, Config.StandingHeight * 0.5f);
                if (Motor.CharacterOverlap(
                        Motor.TransientPosition,
                        Motor.TransientRotation,
                        _probedColliders,
                        Motor.CollidableLayers,
                        QueryTriggerInteraction.Ignore) > 0)
                {
                    // 受阻，继续保持蹲伏
                    Motor.SetCapsuleDimensions(Config.CapsuleRadius, Config.CrouchedHeight, Config.CrouchedHeight * 0.5f);
                    return;
                }

                _isCrouching = false;
                if (MeshRoot != null)
                {
                    var s = MeshRoot.localScale;
                    MeshRoot.localScale = new Vector3(s.x, 1f, s.z);
                }
            }
        }

        // 开始飞扑
        private void StartDash()
        {
            Vector3 dashDir = _moveInputVector.sqrMagnitude > 0.0001f ? _moveInputVector : transform.forward;
            _dashDirection = dashDir.normalized;
            TransitionToState(CharacterState.Dash);

            // 空中飞扑可选地消耗一次跳跃
            if (!Motor.GroundingStatus.IsStableOnGround && Config.AirDashConsumesJump && _jumpConsumed < Config.MaxJumpCount)
            {
                _jumpConsumed = Mathf.Clamp(_jumpConsumed + 1, 0, Config.MaxJumpCount);
            }
        }

        #region Motor API
        // 在角色更新前
        public void BeforeCharacterUpdate(float deltaTime)
        {
        }

        // 更新旋转
        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            Vector3 currentUp = currentRotation * Vector3.up;

            switch (_currentState)
            {
                case CharacterState.Dash:
                    if (_dashDirection.sqrMagnitude > 0.0001f)
                    {
                        Vector3 targetForward = Vector3.Slerp(Motor.CharacterForward, _dashDirection,
                            1 - Mathf.Exp(-Config.OrientationSharpness * deltaTime)).normalized;
                        currentRotation = Quaternion.LookRotation(targetForward, currentUp);
                    }
                    break;

                case CharacterState.WallClimb:
                    if (_lastWallNormal != Vector3.zero)
                    {
                        Vector3 outward = Vector3.ProjectOnPlane(-_lastWallNormal, currentUp).normalized;
                        if (outward.sqrMagnitude > 0.0001f)
                        {
                            Vector3 targetForward = Vector3.Slerp(Motor.CharacterForward, outward,
                                1 - Mathf.Exp(-Config.OrientationSharpness * deltaTime)).normalized;
                            currentRotation = Quaternion.LookRotation(targetForward, currentUp);
                        }
                    }
                    break;

                default:
                    if (_desiredForward.sqrMagnitude > 0.0001f)
                    {
                        Vector3 targetForward = Vector3.Slerp(Motor.CharacterForward, _desiredForward,
                            1 - Mathf.Exp(-Config.OrientationSharpness * deltaTime)).normalized;
                        currentRotation = Quaternion.LookRotation(targetForward, currentUp);
                    }
                    break;
            }

            // 始终将角色上方向对齐世界上（或配置重力方向的反向）
            Vector3 smoothedGravityUp = Vector3.Slerp(currentUp, Vector3.up, 1 - Mathf.Exp(-Config.OrientationSharpness * deltaTime));
            currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityUp) * currentRotation;
        }

        // 更新速度
        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            // 冷却/计时更新
            if (_dashOnCooldown)
            {
                _dashCooldownTimer += deltaTime;
                if (_dashCooldownTimer >= Config.DashCooldown)
                {
                    _dashOnCooldown = false;
                }
            }

            // 接地/空中状态维护
            bool isStableOnGround = Motor.GroundingStatus.IsStableOnGround;
            _jumpedThisFrame = false;
            _timeSinceJumpRequested += deltaTime;
            if (isStableOnGround)
            {
                _timeSinceLastAbleToJump = 0f;
                if (_currentState == CharacterState.Airborne)
                {
                    TransitionToState(CharacterState.Default);
                }
                _jumpConsumed = 0; // 着地重置跳跃计数
            }
            else
            {
                _timeSinceLastAbleToJump += deltaTime;
                if (_currentState == CharacterState.Default)
                {
                    TransitionToState(CharacterState.Airborne);
                }
            }

            switch (_currentState)
            {
                case CharacterState.Dash:
                    {
                        // 飞扑速度沿平面方向，并快速衰减
                        if (_dashTimeRemaining > 0f)
                        {
                            Vector3 targetHorizontal = _dashDirection * Config.DashSpeed;
                            Vector3 currentHorizontal = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp);
                            currentHorizontal = Vector3.Lerp(currentHorizontal, targetHorizontal, 1f - Mathf.Exp(-Config.DashDecay * deltaTime));
                            currentVelocity = currentHorizontal + Vector3.Project(currentVelocity, Motor.CharacterUp);
                            _dashTimeRemaining -= deltaTime;
                        }
                        else
                        {
                            // 飞扑结束，回到空中或地面
                            TransitionToState(isStableOnGround ? CharacterState.Default : CharacterState.Airborne);
                        }
                        // 飞扑不受重力影响太大，轻微施加
                        currentVelocity += Config.Gravity * 0.25f * deltaTime;
                        break;
                    }

                case CharacterState.Ladder:
                    {
                        // 忽略重力，只允许沿上方向移动
                        float forward = Mathf.Max(0f, Vector3.Dot(_moveInputVector, transform.forward));
                        float backward = Mathf.Max(0f, Vector3.Dot(-_moveInputVector, transform.forward));
                        float vertical = (forward - backward) * Config.LadderClimbSpeed;
                        currentVelocity = transform.up * vertical;

                        // 离开梯子条件：远离或无梯子，或跳跃
                        bool noLadderNearby = !DetectLadder(out _);
                        if (noLadderNearby)
                        {
                            TransitionToState(isStableOnGround ? CharacterState.Default : CharacterState.Airborne);
                        }
                        break;
                    }

                case CharacterState.WallClimb:
                    {
                        // 缓慢下滑，保持贴墙
                        _wallGrabTimer += deltaTime;
                        Vector3 verticalVelocity = Vector3.Project(currentVelocity, Motor.CharacterUp);
                        currentVelocity = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp);
                        currentVelocity += -Motor.CharacterUp * Config.WallSlideSpeed;
                        currentVelocity += verticalVelocity * 0.0f; // 清除原有竖直上升

                        // 超时或离开墙体则掉落
                        if (_wallGrabTimer >= Config.MaxWallGrabDuration || !IsStillFacingClimbableWall(out _))
                        {
                            TransitionToState(CharacterState.Airborne);
                        }
                        // 重力小量影响
                        currentVelocity += Config.Gravity * 0.2f * deltaTime;
                        break;
                    }

                case CharacterState.Default:
                case CharacterState.Airborne:
                default:
                    {
                        if (isStableOnGround)
                        {
                            // 地面重新定向速度以顺着地面
                            float currentSpeedMagnitude = currentVelocity.magnitude;
                            Vector3 effectiveGroundNormal = Motor.GroundingStatus.GroundNormal;
                            currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, effectiveGroundNormal) * currentSpeedMagnitude;

                            // 目标速度
                            Vector3 inputRight = Vector3.Cross(_moveInputVector, Motor.CharacterUp);
                            Vector3 reorientedInput = Vector3.Cross(effectiveGroundNormal, inputRight).normalized * _moveInputVector.magnitude;
                            float speedLimit = Config.MaxStableMoveSpeed * (_isCrouching ? Config.CrouchedSpeedMultiplier : 1f);
                            Vector3 targetVelocity = reorientedInput * speedLimit;
                            currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, 1f - Mathf.Exp(-Config.StableMovementSharpness * deltaTime));
                        }
                        else
                        {
                            // 空中添加输入
                            if (_moveInputVector.sqrMagnitude > 0f)
                            {
                                Vector3 addedVelocity = _moveInputVector * Config.AirAcceleration * deltaTime;
                                Vector3 currentPlanarVelocity = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp);
                                
                                // 限制空中速度
                                if (currentPlanarVelocity.magnitude < Config.MaxAirMoveSpeed)
                                {
                                    // 限制总速度不超过最大值
                                    Vector3 newTotal = Vector3.ClampMagnitude(currentPlanarVelocity + addedVelocity, Config.MaxAirMoveSpeed);
                                    addedVelocity = newTotal - currentPlanarVelocity;
                                }
                                else
                                {
                                    // 已经超速时，只允许垂直于当前速度方向的移动（用于转向）
                                    if (Vector3.Dot(currentPlanarVelocity, addedVelocity) > 0f)
                                    {
                                        addedVelocity = Vector3.ProjectOnPlane(addedVelocity, currentPlanarVelocity.normalized);
                                    }
                                }

                                // 贴坡面防止沿斜墙爬升
                                if (Motor.GroundingStatus.FoundAnyGround)
                                {
                                    if (Vector3.Dot(currentVelocity + addedVelocity, addedVelocity) > 0f)
                                    {
                                        Vector3 perpendicularObstructionNormal = Vector3.Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal), Motor.CharacterUp).normalized;
                                        addedVelocity = Vector3.ProjectOnPlane(addedVelocity, perpendicularObstructionNormal);
                                    }
                                }
                                
                                // 应用速度变化
                                currentVelocity += addedVelocity;
                            }

                            // 重力与阻力
                            currentVelocity += Config.Gravity * deltaTime;
                            currentVelocity *= (1f / (1f + (Config.AirDrag * deltaTime)));
                        }

                        // 跳跃处理（缓冲 + 土狼时间 + 多段跳）
                        if (_jumpRequested)
                        {
                            bool canJump =
                                (isStableOnGround || _timeSinceLastAbleToJump <= Config.CoyoteTime) ||
                                (_jumpConsumed < Config.MaxJumpCount);

                            bool bufferValid = _timeSinceJumpRequested <= Config.JumpBufferTime;
                            if (canJump && bufferValid)
                            {
                                Vector3 jumpDirection = Motor.CharacterUp;
                                if (Motor.GroundingStatus.FoundAnyGround && !Motor.GroundingStatus.IsStableOnGround)
                                {
                                    jumpDirection = Motor.GroundingStatus.GroundNormal;
                                }

                                Motor.ForceUnground();
                                currentVelocity += (jumpDirection * Config.JumpUpSpeed) - Vector3.Project(currentVelocity, Motor.CharacterUp);
                                currentVelocity += (_moveInputVector * Config.JumpForwardSpeed);

                                _jumpConsumed = (isStableOnGround || _timeSinceLastAbleToJump <= Config.CoyoteTime)
                                    ? 1 // 起跳视为已使用一次
                                    : Mathf.Clamp(_jumpConsumed + 1, 1, Config.MaxJumpCount);

                                _jumpedThisFrame = true;
                                _jumpRequested = false;
                                _timeSinceJumpRequested = float.PositiveInfinity;

                                // 跳跃可打断飞扑与爬墙/爬梯子
                                if (_currentState == CharacterState.Dash || _currentState == CharacterState.WallClimb || _currentState == CharacterState.Ladder)
                                {
                                    TransitionToState(CharacterState.Airborne);
                                }
                            }
                            else if (!bufferValid)
                            {
                                _jumpRequested = false;
                            }
                        }

                        // 梯子吸附检测（按前进靠近梯子时进入）
                        if (_currentState != CharacterState.Ladder && DetectLadder(out _currentLadder))
                        {
                            // 若玩家朝前输入，吸附
                            if (Vector3.Dot(_moveInputVector, transform.forward) > 0.1f)
                            {
                                TransitionToState(CharacterState.Ladder);
                            }
                        }

                        break;
                    }
            }
        }

        // 在角色更新后
        public void AfterCharacterUpdate(float deltaTime)
        {
            // 解除蹲伏：若当前是蹲伏且玩家未再切换，将在输入触发时处理，这里只维护可站起的空间检测由切换逻辑处理
        }

        // 在接地更新后
        public void PostGroundingUpdate(float deltaTime)
        {
            // 着地/离地事件留空，外部可扩展
        }

        // 是否有效碰撞器
        public bool IsColliderValidForCollisions(Collider coll)
        {
            return true;
        }

        // 地面碰撞
        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }

        // 移动碰撞
        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
            // 空中时若正面撞到可爬墙表面，进入爬墙
            if (!Motor.GroundingStatus.IsStableOnGround && IsClimbableWallNormal(hitNormal))
            {
                _lastWallNormal = hitNormal;
                if (IsStillFacingClimbableWall(out _))
                {
                    TransitionToState(CharacterState.WallClimb);
                }
            }
        }

        // 添加速度
        public void AddVelocity(Vector3 velocity)
        {
            // 提供外部推力接口：合并为瞬时速度增量
            Motor.BaseVelocity += velocity;
        }

        // 处理碰撞稳定性报告
        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
        }

        // 离散碰撞检测
        public void OnDiscreteCollisionDetected(Collider hitCollider)
        {
        }
        #endregion Motor API

        #region State Transition Helper Methods
        // 是否可爬墙

        private bool IsClimbableWallNormal(Vector3 normal)
        {
            // 法线与上方向的夹角大于阈值（接近垂直墙）
            float angle = Vector3.Angle(normal, Vector3.up);
            return angle >= Config.MaxWallClimbAngle * 0.5f; // 宽松一些
        }

        // 是否仍面对可爬墙表面
        private bool IsStillFacingClimbableWall(out RaycastHit hit)
        {
            Vector3 origin = Motor.TransientPosition + Motor.CharacterUp * 0.5f;
            bool didHit = Physics.Raycast(origin, -_lastWallNormal.normalized, out hit, Config.WallStickDistance, Config.ClimbableWallLayer, QueryTriggerInteraction.Ignore);
            return didHit;
        }

        // 检测梯子
        private bool DetectLadder(out Collider ladder)
        {
            ladder = null;
            Vector3 origin = Motor.TransientPosition + Motor.CharacterUp * (Config.CapsuleRadius + 0.1f);
            Vector3 direction = transform.forward;
            int hitCount = Physics.OverlapSphereNonAlloc(origin + direction * Config.LadderDetectionDistance, Config.LadderDetectionRadius, _probedColliders, Config.LadderLayer, QueryTriggerInteraction.Collide);
            for (int i = 0; i < hitCount; i++)
            {
                Collider c = _probedColliders[i];
                if (c == null) continue;
                if (string.IsNullOrEmpty(Config.LadderTag) || c.CompareTag(Config.LadderTag))
                {
                    ladder = c;
                    return true;
                }
            }
            return false;
        }

        #endregion State Transition Helper Methods
    }
}



