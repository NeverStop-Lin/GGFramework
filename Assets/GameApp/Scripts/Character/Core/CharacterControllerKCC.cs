using System;
using KinematicCharacterController;
using UnityEngine;
using Framework.Core.Attributes;

namespace GameApp.Character
{
    /// <summary>
    /// 输入数据结构（由外部输入系统组装）
    /// </summary>
    public struct CharacterInputData
    {
        public Vector2 MoveInput;         // 移动输入（-1到1）或世界空间方向
        public bool JumpDown;             // 跳跃按钮按下
        public Quaternion CameraRotation; // 相机旋转（可选）
        public bool UseCameraRotation;    // 是否使用相机旋转进行坐标转换
    }

    /// <summary>
    /// 使用 KCC 的第三人称角色控制器
    /// 基于时间的跳跃系统，支持：移动、多段跳、相机相对移动
    /// </summary>
    public class CharacterControllerKCC : MonoBehaviour, ICharacterController
    {
        #region Configuration
        [Header("必须组件")]
        [Label("运动器")]
        public KinematicCharacterMotor Motor;
        [Label("网格根节点")]
        public Transform MeshRoot;
        [Label("角色控制器配置")]
        public CharacterControllerConfig Config;
        #endregion

        #region State
        // 跳跃状态结构体
        private struct JumpState
        {
            public bool IsActive;                // 跳跃是否激活
            public float ElapsedTime;            // 已经过的时间
            public int ConsumedCount;            // 已消耗的跳跃次数
            public float CoyoteTimeRemaining;    // 剩余土狼时间
            public float BufferTimeRemaining;    // 剩余跳跃缓冲时间

            public void Reset()
            {
                IsActive = false;
                ElapsedTime = 0f;
                ConsumedCount = 0;
                CoyoteTimeRemaining = 0f;
                BufferTimeRemaining = 0f;
            }
        }

        [Header("运行时状态（只读）")]
        [SerializeField] [Framework.Core.Attributes.ReadOnly] [Label("当前状态")] 
        private CharacterState _currentState = CharacterState.Default;
        
        [SerializeField] [Framework.Core.Attributes.ReadOnly] [Label("跳跃激活")] 
        private bool _jumpStateActive = false;

        // 公共查询接口
        public CharacterState CurrentState => _currentState;

        // 私有状态
        private JumpState _jumpState;
        private Vector3 _moveInputVector = Vector3.zero;
        private Vector3 _desiredForward = Vector3.forward;
        private Camera _mainCamera;
        #endregion

        #region Unity Lifecycle
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
            _jumpState.Reset();
        }
        #endregion

        #region Public Interface
        /// <summary>
        /// 设置相机引用（用于获取相机旋转）
        /// </summary>
        public void SetCamera(Camera camera)
        {
            _mainCamera = camera;
        }

        /// <summary>
        /// 设置输入
        /// </summary>
        public void SetInput(ref CharacterInputData input)
        {
            // 1. 处理移动输入（支持两种模式）
            if (input.UseCameraRotation)
            {
                // 模式1：原始输入 + 相机旋转转换
                Quaternion cameraRotation = input.CameraRotation != Quaternion.identity
                    ? input.CameraRotation
                    : (_mainCamera != null ? _mainCamera.transform.rotation : Quaternion.identity);

                Vector3 cameraPlanarDirection =
                    Vector3.ProjectOnPlane(cameraRotation * Vector3.forward, Motor.CharacterUp).normalized;
                if (cameraPlanarDirection.sqrMagnitude == 0f)
                {
                    cameraPlanarDirection =
                        Vector3.ProjectOnPlane(cameraRotation * Vector3.up, Motor.CharacterUp).normalized;
                }

                Quaternion cameraPlanarRotation = Quaternion.LookRotation(cameraPlanarDirection, Motor.CharacterUp);
                Vector3 rawInput = new Vector3(input.MoveInput.x, 0f, input.MoveInput.y);
                _moveInputVector = cameraPlanarRotation * Vector3.ClampMagnitude(rawInput, 1f);
            }
            else
            {
                // 模式2：已转换的世界空间输入
                _moveInputVector = new Vector3(input.MoveInput.x, 0f, input.MoveInput.y);
                if (_moveInputVector.sqrMagnitude > 1f)
                {
                    _moveInputVector.Normalize();
                }
            }

            // 2. 朝向：取移动方向
            if (_moveInputVector.sqrMagnitude > 0.0001f)
            {
                _desiredForward = _moveInputVector;
            }

            // 3. 跳跃缓冲
            if (input.JumpDown)
            {
                _jumpState.BufferTimeRemaining = Config.JumpBufferTime;
            }
        }
        #endregion

        #region Motor Interface
        public void BeforeCharacterUpdate(float deltaTime)
        {
        }

        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            Vector3 currentUp = currentRotation * Vector3.up;

            if (_desiredForward.sqrMagnitude > 0.0001f)
            {
                // Slerp 球形插值
                Vector3 targetForward = Vector3.Slerp(Motor.CharacterForward, _desiredForward,
                    1f - Mathf.Exp(-Config.OrientationSharpness * deltaTime)).normalized;
                currentRotation = Quaternion.LookRotation(targetForward, currentUp);
            }

            // 对齐世界上方向
            Vector3 smoothedGravityUp = Vector3.Slerp(currentUp, Vector3.up,
                                                      1f - Mathf.Exp(-Config.OrientationSharpness * deltaTime));
            currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityUp) * currentRotation;
        }

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            bool isStableOnGround = Motor.GroundingStatus.IsStableOnGround;

            // 1. 更新土狼时间和跳跃缓冲
            UpdateJumpTimers(isStableOnGround, deltaTime);

            // 2. 状态维护（着地/离地转换）
            UpdateCharacterState(isStableOnGround);

            // 3. 跳跃处理
            HandleJumping(ref currentVelocity, isStableOnGround, deltaTime);

            // 4. 移动处理（地面/空中）
            HandleMovement(ref currentVelocity, isStableOnGround, deltaTime);

            // 5. 重力施加
            currentVelocity += Config.Gravity * deltaTime;

            // 更新调试显示
            _jumpStateActive = _jumpState.IsActive;
        }

        public void AfterCharacterUpdate(float deltaTime)
        {
        }

        public void PostGroundingUpdate(float deltaTime)
        {
        }

        public bool IsColliderValidForCollisions(Collider coll)
        {
            return true;
        }

        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }

        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
            // 检测头顶碰撞（碰撞回调方式）
            if (Vector3.Dot(hitNormal, Motor.CharacterUp) < -0.7f)
            {
                if (_jumpState.IsActive)
                {
                    _jumpState.IsActive = false;
                    if (!Motor.GroundingStatus.IsStableOnGround)
                    {
                        TransitionToState(CharacterState.Airborne);
                    }
                }
            }
        }

        public void AddVelocity(Vector3 velocity)
        {
            Motor.BaseVelocity += velocity;
        }

        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
        }

        public void OnDiscreteCollisionDetected(Collider hitCollider)
        {
        }
        #endregion

        #region Internal Logic
        private void UpdateJumpTimers(bool isGrounded, float deltaTime)
        {
            _jumpState.BufferTimeRemaining -= deltaTime;

            if (isGrounded)
            {
                _jumpState.CoyoteTimeRemaining = Config.CoyoteTime;
                _jumpState.ConsumedCount = 0;
            }
            else
            {
                _jumpState.CoyoteTimeRemaining -= deltaTime;
            }
        }

        private void UpdateCharacterState(bool isGrounded)
        {
            if (isGrounded && (_currentState == CharacterState.Airborne || _currentState == CharacterState.Jumping))
            {
                TransitionToState(CharacterState.Default);
            }
            else if (!isGrounded && _currentState == CharacterState.Default && !_jumpState.IsActive)
            {
                TransitionToState(CharacterState.Airborne);
            }
        }

        private void HandleJumping(ref Vector3 currentVelocity, bool isGrounded, float deltaTime)
        {
            // 检查是否可以跳跃
            bool canJump = (isGrounded || _jumpState.CoyoteTimeRemaining > 0f) ||
                           (_jumpState.ConsumedCount < Config.MaxJumpCount);
            bool bufferValid = _jumpState.BufferTimeRemaining > 0f;

            // 移除 !_jumpState.IsActive 条件，允许打断正在进行的跳跃
            if (canJump && bufferValid)
            {
                // 开始新跳跃
                _jumpState.IsActive = true;
                _jumpState.ElapsedTime = 0f;
                _jumpState.BufferTimeRemaining = 0f;

                if (isGrounded || _jumpState.CoyoteTimeRemaining > 0f)
                {
                    _jumpState.ConsumedCount = 1;
                }
                else
                {
                    _jumpState.ConsumedCount++;
                }

                TransitionToState(CharacterState.Jumping);
                Motor.ForceUnground();

                // 清除所有垂直分量（向上和向下都清除），确保每次跳跃高度一致
                Vector3 verticalVelocity = Vector3.Project(currentVelocity, Motor.CharacterUp);
                currentVelocity -= verticalVelocity;
            }

            // 施加跳跃力（上升期间直接设置垂直速度，下落期间让重力自然作用）
            if (_jumpState.IsActive)
            {
                _jumpState.ElapsedTime += deltaTime;
                float progress = _jumpState.ElapsedTime / Config.JumpDuration;

                if (progress < 1.0f)
                {
                    AnimationCurve curve = Config.JumpSpeedCurve ?? GetDefaultJumpCurve();
                    float speedMultiplier = curve.Evaluate(progress);
                    float targetVerticalSpeed = Config.JumpSpeed * speedMultiplier;

                    // 只在上升期间（曲线值 > 0）赋值垂直速度
                    if (speedMultiplier > 0f)
                    {
                        // 分离水平和垂直速度
                        Vector3 verticalVelocity = Vector3.Project(currentVelocity, Motor.CharacterUp);
                        Vector3 horizontalVelocity = currentVelocity - verticalVelocity;

                        // 直接设置垂直速度（赋值，不累积）
                        verticalVelocity = Motor.CharacterUp * targetVerticalSpeed;

                        // 重新组合
                        currentVelocity = horizontalVelocity + verticalVelocity;
                    }
                    // 否则不干预垂直速度，让重力自然作用（下落期间）
                }
                else
                {
                    // 跳跃结束
                    _jumpState.IsActive = false;
                    if (!isGrounded)
                    {
                        TransitionToState(CharacterState.Airborne);
                    }
                }
            }
        }

        private void HandleMovement(ref Vector3 currentVelocity, bool isGrounded, float deltaTime)
        {
            if (isGrounded)
            {
                // 地面移动（融合 DivCharacterController 的切线计算）
                float currentSpeedMagnitude = currentVelocity.magnitude;
                Vector3 effectiveGroundNormal = Motor.GroundingStatus.GroundNormal;
                currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, effectiveGroundNormal) * currentSpeedMagnitude;

                if (_moveInputVector.sqrMagnitude > 0f)
                {
                    // 有输入：线性加速/减速
                    Vector3 inputRight = Vector3.Cross(_moveInputVector, Motor.CharacterUp);
                    Vector3 reorientedInput = Vector3.Cross(effectiveGroundNormal, inputRight).normalized * _moveInputVector.magnitude;
                    Vector3 targetVelocity = reorientedInput * Config.MaxStableMoveSpeed;

                    Vector3 velocityDiff = targetVelocity - currentVelocity;
                    float maxSpeedChange = Config.StableMovementSharpness * deltaTime;
                    
                    if (velocityDiff.magnitude <= maxSpeedChange)
                    {
                        currentVelocity = targetVelocity;
                    }
                    else
                    {
                        currentVelocity += velocityDiff.normalized * maxSpeedChange;
                    }
                }
                else
                {
                    // 无输入：线性减速，精确停止
                    float speedDecrease = Config.GroundFriction * deltaTime;
                    float currentSpeed = currentVelocity.magnitude;

                    if (currentSpeed <= speedDecrease)
                    {
                        currentVelocity = Vector3.zero;
                    }
                    else
                    {
                        currentVelocity -= currentVelocity.normalized * speedDecrease;
                    }
                }
            }
            else
            {
                // 分离垂直和水平速度
                Vector3 verticalVelocity = Vector3.Project(currentVelocity, Motor.CharacterUp);
                Vector3 horizontalVelocity = currentVelocity - verticalVelocity;

                // 空中移动（线性加速，快速响应，保留地面速度）
                if (_moveInputVector.sqrMagnitude > 0f)
                {
                    // 计算目标水平速度
                    Vector3 targetHorizontalVelocity = _moveInputVector * Config.MaxAirMoveSpeed;

                    // 防爬坡
                    if (Motor.GroundingStatus.FoundAnyGround)
                    {
                        Vector3 perpenticularObstructionNormal =
                            Vector3.Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal),
                                         Motor.CharacterUp).normalized;
                        targetHorizontalVelocity = Vector3.ProjectOnPlane(targetHorizontalVelocity, perpenticularObstructionNormal);
                    }

                    // 检查输入方向上的当前速度
                    float currentSpeedInInputDir = Vector3.Dot(horizontalVelocity, _moveInputVector.normalized);

                    // 只在需要加速或转向时才修改速度（不减速已有的更快速度）
                    if (currentSpeedInInputDir < Config.MaxAirMoveSpeed)
                    {
                        // 线性插值向目标速度移动
                        Vector3 velocityDiff = targetHorizontalVelocity - horizontalVelocity;
                        float maxSpeedChange = Config.AirAcceleration * deltaTime;

                        if (velocityDiff.magnitude <= maxSpeedChange)
                        {
                            horizontalVelocity = targetHorizontalVelocity;
                        }
                        else
                        {
                            horizontalVelocity += velocityDiff.normalized * maxSpeedChange;
                        }
                    }
                }
                else
                {
                    // 无输入时施加空中阻力（线性减速，精确停止）
                    float speedDecrease = Config.AirDrag * deltaTime;
                    float currentSpeed = horizontalVelocity.magnitude;

                    if (currentSpeed <= speedDecrease)
                    {
                        horizontalVelocity = Vector3.zero;
                    }
                    else
                    {
                        horizontalVelocity -= horizontalVelocity.normalized * speedDecrease;
                    }
                }

                // 重新组合（垂直速度不变）
                currentVelocity = horizontalVelocity + verticalVelocity;
            }
        }

        private AnimationCurve GetDefaultJumpCurve()
        {
            // 创建默认平台跳曲线：(0, 1.0) → (0.8, 0.8) → (1.0, 0)
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(new Keyframe(0f, 1.0f));
            curve.AddKey(new Keyframe(0.8f, 0.8f));
            curve.AddKey(new Keyframe(1.0f, 0f));

            // 设置为平滑插值
            for (int i = 0; i < curve.keys.Length; i++)
            {
                curve.SmoothTangents(i, 0f);
            }

            return curve;
        }

        private void TransitionToState(CharacterState newState)
        {
            _currentState = newState;
        }
        #endregion
    }
}
