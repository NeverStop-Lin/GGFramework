using UnityEngine;
using KinematicCharacterController;
using GameApp.Character;
using Framework.Core.Attributes;
using Newtonsoft.Json;

namespace GameApp.Demo
{
    /// <summary>
    /// 玩家角色输入数据结构
    /// </summary>
    public struct DemoPlayerInputs
    {
        public float MoveAxisForward;  // 前后移动输入（-1到1）
        public float MoveAxisRight;    // 左右移动输入（-1到1）
        public Quaternion CameraRotation; // 相机旋转
        public bool JumpDown; // 跳跃按键按下
        public bool DashDown; // 飞扑按键按下（左Shift）
    }

    /// <summary>
    /// 基于教程1-2的Demo角色控制器
    /// 实现基础移动和重力系统
    /// </summary>
    public class DemoCharacterController : MonoBehaviour, ICharacterController
    {
        #region 常量
        private const float MIN_SQR_MAGNITUDE_THRESHOLD = 0.0001f; // 最小向量长度的平方阈值
        private const float INPUT_CLAMP_MAGNITUDE = 1f; // 输入向量限制长度
        #endregion

        #region 组件引用
        [Header("组件引用")]
        [Label("运动器")]
        public KinematicCharacterMotor Motor;

        [Label("角色控制器配置")]
        public CharacterControllerConfig Config;

        [Label("网格根节点")]
        public Transform MeshRoot;
        #endregion

        #region 地面移动参数
        [Header("地面移动参数")]
        [Label("最大地面移动速度")]
        public float MaxStableMoveSpeed = 10f;

        [Label("地面移动平滑度", "控制加速/减速的平滑程度，数值越大响应越快（平滑度越低）")]
        public float StableMovementSmoothness = 15f;

        [Label("转向平滑度", "控制角色转向的平滑程度，数值越大转向越快（平滑度越低）")]
        public float RotationSmoothness = 10f;
        #endregion

        #region 空中移动参数
        [Header("Air Movement")]
        [Label("最大空中移动速度")]
        public float MaxAirMoveSpeed = 10f;

        [Label("空中加速")]
        public float AirAccelerationSpeed = 5f;


        [Label("空气阻力")]
        public float Drag = 0.1f;

        #endregion

        #region 杂项参数
        [Header("杂项参数")]
        [Label("重力")]
        public Vector3 Gravity = new(0, -30f, 0);
        #endregion

        #region 跳跃参数
        [Header("跳跃参数")]
        [Label("最大跳跃次数", "支持多段跳，1=单跳，2=二段跳")]
        public int MaxJumpCount = 2;
        [Label("跳跃持续时间", "跳跃力施加的时间")]
        public float JumpDuration = 0.4f;
        [Label("跳跃力度", "跳跃时的最大垂直速度（米/秒），配合曲线控制跳跃轨迹")]
        public float JumpForce = 10f;
        [Label("跳跃速度曲线", "X=时间进度0-1，Y=速度倍率0-1")]
        public AnimationCurve JumpSpeedCurve;
        [Label("跳跃缓冲时间", "提前按下跳跃的缓冲时间")]
        public float JumpBufferTime = 0.15f;
        #endregion

        #region 飞扑参数
        [Header("飞扑参数")]
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
        #endregion

        #region 调试信息
        [Header("调试信息")]
        [Framework.Core.Attributes.ReadOnly]
        [SerializeField]
        [Label("水平移动速度", "米/秒",true)]
        private float _horizontalMoveSpeed;

        [Framework.Core.Attributes.ReadOnly]
        [SerializeField]
        [Label("垂直速度", "米/秒",true)]
        private float _verticalVelocity;

        [Framework.Core.Attributes.ReadOnly]
        [SerializeField]
        [Label("总移动速度", "米/秒",true)]
        private float _totalMoveSpeed;

        [Framework.Core.Attributes.ReadOnly]
        [SerializeField]
        [Label("是否在地面","",true)]
        private bool _isOnGround;

        [Framework.Core.Attributes.ReadOnly]
        [SerializeField]
        [Label("移动输入强度", "0-1",true)]
        private float _moveInputMagnitude;

        [Framework.Core.Attributes.ReadOnly]
        [SerializeField]
        [Label("飞扑状态", "",true)]
        private bool _isDashingDebug;

        [Framework.Core.Attributes.ReadOnly]
        [SerializeField]
        [Label("可用飞扑次数", "",true)]
        private int _availableDashCountDebug;

        [Framework.Core.Attributes.ReadOnly]
        [SerializeField]
        [Label("飞扑速度", "米/秒",true)]
        private float _dashCurrentSpeed;
        #endregion

        #region 私有字段
        private Vector3 _moveInputVector; // 世界坐标系的移动输入

        // 跳跃相关字段
        private bool _jumpRequested; // 跳跃请求标记
        private float _timeSinceJumpRequested = Mathf.Infinity; // 跳跃请求后经过的时间（用于缓冲）
        private int _currentJumpCount; // 当前跳跃次数
        private float _jumpElapsedTime; // 当前跳跃已持续的时间
        private bool _isJumping; // 是否正在跳跃中
        private bool _jumpedThisFrame; // 本帧是否跳跃了

        // 飞扑相关字段
        private bool _isDashing; // 是否在飞扑状态
        private bool _dashRequested; // 飞扑请求标记
        private int _availableDashCount; // 可用飞扑次数
        private Vector3 _dashVelocity; // 飞扑速度向量
        #endregion

        #region Unity生命周期
        private void Start()
        {
            ValidateComponents();
            RegisterToMotor();
            AppilyConfigValues();
            ValidateConfig();
        }
        #endregion

        #region 初始化
        /// <summary>
        /// 验证必需组件是否存在
        /// </summary>
        private void ValidateComponents()
        {
            if (Motor == null)
            {
                throw new System.InvalidOperationException("KinematicCharacterMotor is required");
            }
        }
        /// <summary>
        /// 验证配置是否有效
        /// </summary>
        private void ValidateConfig()
        {
            if (JumpSpeedCurve == null || JumpSpeedCurve.keys.Length == 0)
            {
                JumpSpeedCurve = CreateDefaultJumpSpeedCurve();
            }
        }
        /// <summary>
        /// 注册控制器到运动器
        /// </summary>
        private void RegisterToMotor()
        {
            Motor.CharacterController = this;
        }

        /// <summary>
        /// 应用配置资源中的参数值
        /// </summary>
        private void AppilyConfigValues()
        {
            if (Config == null)
            {
                return;
            }

            MaxStableMoveSpeed = Config.MaxStableMoveSpeed;
            StableMovementSmoothness = Config.StableMovementSmoothness;
            RotationSmoothness = Config.RotationSmoothness;
            MaxAirMoveSpeed = Config.MaxAirMoveSpeed;
            AirAccelerationSpeed = Config.AirAcceleration;
            Drag = Config.AirDrag;
            Gravity = Config.Gravity;

            // 跳跃参数
            MaxJumpCount = Config.MaxJumpCount;
            JumpDuration = Config.JumpDuration;
            JumpForce = Config.JumpForce;
            JumpSpeedCurve = Config.JumpSpeedCurve;
            JumpBufferTime = Config.JumpBufferTime;

            // 飞扑参数
            DashSpeedMultiplier = Config.DashSpeedMultiplier;
            DashMinSpeed = Config.DashMinSpeed;
            DashMaxSpeed = Config.DashMaxSpeed;
            DashGroundSmoothness = Config.DashGroundSmoothness;
            DashRotationSmoothness = Config.DashRotationSmoothness;
        }

        /// <summary>
        /// 创建默认的跳跃速度曲线
        /// </summary>
        /// <returns>默认的跳跃速度曲线</returns>
        private AnimationCurve CreateDefaultJumpSpeedCurve()
        {
            AnimationCurve curve = new AnimationCurve();

            // 第一个关键帧：时间0.0，值1.0（跳跃开始时的最大速度）

            Keyframe keyframe0 = new Keyframe(0.0f, 1.0f);
            keyframe0.inTangent = -2.005989f;
            keyframe0.outTangent = -2.005989f;
            keyframe0.inWeight = 0.0f;
            keyframe0.outWeight = 0.147064656f;
            keyframe0.weightedMode = WeightedMode.None;
            curve.AddKey(keyframe0);

            // 第二个关键帧：时间1.0，值0.0（跳跃结束时的速度为0）

            Keyframe keyframe1 = new Keyframe(1.0f, 0.0f);
            keyframe1.inTangent = -2.02409434f;
            keyframe1.outTangent = -2.02409434f;
            keyframe1.inWeight = 0.1518442f;
            keyframe1.outWeight = 0.0f;
            keyframe1.weightedMode = WeightedMode.None;
            curve.AddKey(keyframe1);

            // 设置曲线的包装模式

            curve.preWrapMode = WrapMode.Clamp;
            curve.postWrapMode = WrapMode.Clamp;


            return curve;
        }
        #endregion

        #region 输入处理
        /// <summary>
        /// 设置角色输入（由DemoPlayer每帧调用）
        /// </summary>
        public void SetInputs(ref DemoPlayerInputs inputs)
        {


            Vector3 rawMoveInput = ConvertRawInputToVector(inputs);
            Vector3 cameraDirection = CalculateCameraPlanarDirection(inputs.CameraRotation);
            Quaternion cameraRotation = Quaternion.LookRotation(cameraDirection, Motor.CharacterUp);

            _moveInputVector = cameraRotation * rawMoveInput;

            // 处理跳跃输入
            if (inputs.JumpDown)
            {
                _timeSinceJumpRequested = 0f;
                _jumpRequested = true;
            }

            // 处理飞扑输入（跳跃后可以触发）
            if (inputs.DashDown && _availableDashCount > 0 && !_isDashing)
            {
                _dashRequested = true;
            }
        }

        /// <summary>
        /// 将原始输入转换为向量（限制长度为1）
        /// </summary>
        private Vector3 ConvertRawInputToVector(DemoPlayerInputs inputs)
        {
            Vector3 rawInput = new Vector3(inputs.MoveAxisRight, 0f, inputs.MoveAxisForward);
            return Vector3.ClampMagnitude(rawInput, INPUT_CLAMP_MAGNITUDE);
        }

        /// <summary>
        /// 计算相机在角色水平面上的方向
        /// </summary>
        private Vector3 CalculateCameraPlanarDirection(Quaternion cameraRotation)
        {
            // 将相机前方向投影到水平面
            Vector3 cameraForward = cameraRotation * Vector3.forward;
            Vector3 planarDirection = Vector3.ProjectOnPlane(cameraForward, Motor.CharacterUp).normalized;

            // 如果投影后长度为0（相机垂直），使用相机的上方向
            if (planarDirection.sqrMagnitude < MIN_SQR_MAGNITUDE_THRESHOLD)
            {
                Vector3 cameraUp = cameraRotation * Vector3.up;
                planarDirection = Vector3.ProjectOnPlane(cameraUp, Motor.CharacterUp).normalized;
            }

            return planarDirection;
        }
        #endregion

        #region 运动器接口 - 更新循环处理
        public void BeforeCharacterUpdate(float deltaTime)
        {
            // 预留：移动前的预处理逻辑
        }

        #region 更新旋转处理
        /// <summary>
        /// 更新角色旋转（唯一可设置旋转的地方）
        /// </summary>
        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            // 根据状态选择转向平滑度
            float rotationSmoothness = _isDashing ? DashRotationSmoothness : RotationSmoothness;

            // 如果移动输入方向无效或平滑度为0，不更新旋转
            if (_moveInputVector.sqrMagnitude <= MIN_SQR_MAGNITUDE_THRESHOLD || rotationSmoothness <= 0f)
            {
                return;
            }

            // 计算目标旋转方向（投影到水平面，只保留Y轴旋转）
            Vector3 targetDirection = Vector3.ProjectOnPlane(_moveInputVector, Motor.CharacterUp).normalized;


            if (targetDirection.sqrMagnitude < MIN_SQR_MAGNITUDE_THRESHOLD)
            {
                return;
            }

            // 使用平滑插值旋转到目标方向（只旋转Y轴）
            Vector3 currentForward = Vector3.ProjectOnPlane(Motor.CharacterForward, Motor.CharacterUp).normalized;
            Vector3 smoothedDirection = CalculateSmoothedRotationDirection(currentForward, targetDirection, deltaTime, rotationSmoothness);
            currentRotation = Quaternion.LookRotation(smoothedDirection, Motor.CharacterUp);
        }

        /// <summary>
        /// 计算平滑后的旋转方向（仅在水平面）
        /// </summary>
        private Vector3 CalculateSmoothedRotationDirection(Vector3 currentDirection, Vector3 targetDirection, float deltaTime, float rotationSmoothness)
        {
            float smoothFactor = 1f - Mathf.Exp(-rotationSmoothness * deltaTime);
            return Vector3.Slerp(currentDirection, targetDirection, smoothFactor).normalized;
        }
        #endregion

        /// <summary>
        /// 更新角色速度（唯一可设置速度的地方）
        /// </summary>
        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            // 1. 处理飞扑请求（跳跃后可触发）
            HandleDashRequest(ref currentVelocity);

            // 2. 处理跳跃（可打断飞扑）
            HandleJumping(ref currentVelocity, deltaTime);

            // 3. 根据状态处理移动
            if (_isDashing)
            {
                HandleDashMovement(ref currentVelocity, deltaTime);
            }
            else if (Motor.GroundingStatus.IsStableOnGround)
            {
                HandleStableMovement(ref currentVelocity, deltaTime);
            }
            else
            {
                HandleAirMovement(ref currentVelocity, deltaTime);
            }

            // 更新调试信息
            UpdateDebugInfo(currentVelocity);
        }

        /// <summary>
        /// 更新调试信息（在Inspector中显示）
        /// </summary>
        private void UpdateDebugInfo(Vector3 currentVelocity)
        {
            // 计算水平速度（投影到水平面）
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp);
            _horizontalMoveSpeed = horizontalVelocity.magnitude;

            // 计算垂直速度
            Vector3 verticalVelocity = Vector3.Project(currentVelocity, Motor.CharacterUp);
            _verticalVelocity = Vector3.Dot(verticalVelocity, Motor.CharacterUp);

            // 总速度
            _totalMoveSpeed = currentVelocity.magnitude;

            // 是否在地面
            _isOnGround = Motor.GroundingStatus.IsStableOnGround;

            // 移动输入强度
            _moveInputMagnitude = _moveInputVector.magnitude;

            // 飞扑调试信息
            _isDashingDebug = _isDashing;
            _availableDashCountDebug = _availableDashCount;
            _dashCurrentSpeed = _dashVelocity.magnitude;
        }

        #region 处理地面移动
        /// <summary>
        /// 处理地面移动
        /// </summary>
        private void HandleStableMovement(ref Vector3 currentVelocity, float deltaTime)
        {
            // 重新定向速度到地面坡度上
            ReorientVelocityOnGround(ref currentVelocity);

            // 根据是否有输入，分别处理加速和减速
            if (_moveInputVector.sqrMagnitude > MIN_SQR_MAGNITUDE_THRESHOLD)
            {
                // 有输入：计算目标速度并平滑过渡
                Vector3 targetVelocity = CalculateTargetGroundVelocity();
                SmoothVelocityToTarget(ref currentVelocity, targetVelocity, StableMovementSmoothness, deltaTime);
            }
            else
            {
                // 目标速度为0，通过平滑插值自动减速
                Vector3 targetVelocity = Vector3.zero;
                SmoothVelocityToTarget(ref currentVelocity, targetVelocity, StableMovementSmoothness, deltaTime);
            }
        }

        /// <summary>
        /// 将速度重新定向到地面坡度上
        /// </summary>
        private void ReorientVelocityOnGround(ref Vector3 currentVelocity)
        {
            Vector3 groundNormal = Motor.GroundingStatus.GroundNormal;
            Vector3 tangentDirection = Motor.GetDirectionTangentToSurface(currentVelocity, groundNormal);
            currentVelocity = tangentDirection * currentVelocity.magnitude;
        }

        /// <summary>
        /// 计算目标地面移动速度
        /// </summary>
        private Vector3 CalculateTargetGroundVelocity()
        {
            Vector3 groundNormal = Motor.GroundingStatus.GroundNormal;
            Vector3 inputRight = Vector3.Cross(_moveInputVector, Motor.CharacterUp);
            Vector3 reorientedInput = Vector3.Cross(groundNormal, inputRight).normalized * _moveInputVector.magnitude;
            return reorientedInput * MaxStableMoveSpeed;
        }
        /// <summary>
        /// 平滑地将速度过渡到目标速度
        /// </summary>
        private void SmoothVelocityToTarget(ref Vector3 currentVelocity, Vector3 targetVelocity, float sharpness, float deltaTime)
        {
            float smoothFactor = 1f - Mathf.Exp(-sharpness * deltaTime);
            currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, smoothFactor);
        }
        #endregion

        /// <summary>
        /// 处理空中移动
        /// </summary>
        private void HandleAirMovement(ref Vector3 currentVelocity, float deltaTime)
        {
            // 根据是否有输入，分别处理加速和减速
            if (_moveInputVector.sqrMagnitude > MIN_SQR_MAGNITUDE_THRESHOLD)
            {
                // 有输入：应用移动输入
                ApplyAirMovementInput(ref currentVelocity, deltaTime);
            }
            else
            {
                // 无输入：将水平速度减速到0（空气阻力会处理垂直速度）
                Vector3 horizontalVelocity = Vector3.ProjectOnPlane(currentVelocity, Gravity.normalized);
                if (horizontalVelocity.sqrMagnitude > MIN_SQR_MAGNITUDE_THRESHOLD)
                {
                    // 先提取垂直分量（基于原始值）
                    Vector3 verticalVelocity = currentVelocity - horizontalVelocity;
                    // 然后平滑减速水平速度到0
                    SmoothVelocityToTarget(ref horizontalVelocity, Vector3.zero, AirAccelerationSpeed, deltaTime);
                    // 重新组合速度（保留垂直分量）
                    currentVelocity = horizontalVelocity + verticalVelocity;
                }
            }

            ApplyGravity(ref currentVelocity, deltaTime);
            ApplyAirDrag(ref currentVelocity, deltaTime);
        }

        /// <summary>
        /// 应用空中移动输入
        /// </summary>
        private void ApplyAirMovementInput(ref Vector3 currentVelocity, float deltaTime)
        {
            Vector3 targetVelocity = _moveInputVector * MaxAirMoveSpeed;

            // 防止在非稳定坡面上攀爬
            if (!Motor.GroundingStatus.IsStableOnGround)
            {
                Vector3 obstructionNormal = CalculateObstructionNormal();
                // 只限制朝向障碍物的移动，允许远离障碍物
                float dotProduct = Vector3.Dot(targetVelocity, obstructionNormal);
                if (dotProduct < 0f)
                {
                    // 速度朝向障碍物方向，应用限制
                    targetVelocity = Vector3.ProjectOnPlane(targetVelocity, obstructionNormal);
                }
            }

            // 只在水平方向加速
            Vector3 velocityDiff = Vector3.ProjectOnPlane(targetVelocity - currentVelocity, Gravity);
            currentVelocity += velocityDiff * AirAccelerationSpeed * deltaTime;
        }

        /// <summary>
        /// 计算阻碍法线（防止攀爬）
        /// </summary>
        private Vector3 CalculateObstructionNormal()
        {
            Vector3 groundNormal = Motor.GroundingStatus.GroundNormal;
            Vector3 characterUp = Motor.CharacterUp;
            return Vector3.Cross(Vector3.Cross(characterUp, groundNormal), characterUp).normalized;
        }

        /// <summary>
        /// 施加重力
        /// </summary>
        private void ApplyGravity(ref Vector3 currentVelocity, float deltaTime)
        {
            currentVelocity += Gravity * deltaTime;
        }

        /// <summary>
        /// 应用空气阻力
        /// </summary>
        private void ApplyAirDrag(ref Vector3 currentVelocity, float deltaTime)
        {
            float dragFactor = 1f / (1f + Drag * deltaTime);
            currentVelocity *= dragFactor;
        }
        #endregion

        #region 处理飞扑
        /// <summary>
        /// 处理飞扑请求
        /// </summary>
        private void HandleDashRequest(ref Vector3 currentVelocity)
        {
            if (!_dashRequested) return;

            _dashRequested = false;

            // 计算飞扑方向（基于当前输入）
            Vector3 dashDirection = _moveInputVector.sqrMagnitude > MIN_SQR_MAGNITUDE_THRESHOLD
                ? _moveInputVector.normalized
                : Vector3.ProjectOnPlane(Motor.CharacterForward, Motor.CharacterUp).normalized;

            // 计算飞扑速度 = 固定系数 × 当前输入值，然后限制在最小/最大范围
            float inputMagnitude = _moveInputVector.magnitude;
            float dashSpeed = Mathf.Clamp(
                inputMagnitude * DashSpeedMultiplier * MaxStableMoveSpeed,
                DashMinSpeed,
                DashMaxSpeed
            );

            // 如果输入为零，使用默认速度
            if (inputMagnitude < MIN_SQR_MAGNITUDE_THRESHOLD)
            {
                dashSpeed = DashMinSpeed;
            }

            // 设置飞扑速度向量
            _dashVelocity = dashDirection * dashSpeed;

            // 进入飞扑状态
            _isDashing = true;
            _availableDashCount--;
        }

        /// <summary>
        /// 处理飞扑移动
        /// </summary>
        private void HandleDashMovement(ref Vector3 currentVelocity, float deltaTime)
        {
            bool isOnGround = Motor.GroundingStatus.IsStableOnGround;

            if (isOnGround)
            {
                // 在地面：平滑减速
                float currentSpeed = _dashVelocity.magnitude;
                
                // 如果当前速度已经小于等于最小速度，保持静止但仍在飞扑状态
                if (currentSpeed <= DashMinSpeed)
                {
                    currentVelocity = Vector3.zero;
                    _dashVelocity = Vector3.zero;
                    // 保持飞扑状态，只能通过跳跃打断
                    return;
                }

                float targetSpeed = Mathf.Max(currentSpeed - DashGroundSmoothness * deltaTime, DashMinSpeed);

                // 处理方向控制（只影响方向，不影响速度）
                if (_moveInputVector.sqrMagnitude > MIN_SQR_MAGNITUDE_THRESHOLD)
                {
                    Vector3 inputDirection = _moveInputVector.normalized;
                    Vector3 currentDirection = _dashVelocity.normalized;
                    float smoothFactor = 1f - Mathf.Exp(-DashRotationSmoothness * deltaTime);
                    Vector3 newDirection = Vector3.Slerp(currentDirection, inputDirection, smoothFactor).normalized;
                    _dashVelocity = newDirection * targetSpeed;
                }
                else
                {
                    _dashVelocity = _dashVelocity.normalized * targetSpeed;
                }

                // 贴合地面
                _dashVelocity = Vector3.ProjectOnPlane(_dashVelocity, Motor.GroundingStatus.GroundNormal);

                // 应用速度
                currentVelocity = _dashVelocity;
            }
            else
            {
                // 在空中：使用正常空中移动逻辑
                // 保持飞扑水平速度
                Vector3 horizontalVelocity = Vector3.ProjectOnPlane(_dashVelocity, Motor.CharacterUp);
                Vector3 verticalVelocity = Vector3.Project(currentVelocity, Motor.CharacterUp);

                // 处理方向控制
                if (_moveInputVector.sqrMagnitude > MIN_SQR_MAGNITUDE_THRESHOLD)
                {
                    Vector3 inputDirection = Vector3.ProjectOnPlane(_moveInputVector, Motor.CharacterUp).normalized;
                    Vector3 currentDirection = horizontalVelocity.normalized;
                    float smoothFactor = 1f - Mathf.Exp(-DashRotationSmoothness * deltaTime);
                    Vector3 newDirection = Vector3.Slerp(currentDirection, inputDirection, smoothFactor).normalized;
                    horizontalVelocity = newDirection * horizontalVelocity.magnitude;
                }

                _dashVelocity = horizontalVelocity;
                currentVelocity = horizontalVelocity + verticalVelocity;

                // 应用重力和空气阻力
                ApplyGravity(ref currentVelocity, deltaTime);
                ApplyAirDrag(ref currentVelocity, deltaTime);
            }
        }
        #endregion

        #region 处理跳跃
        /// <summary>
        /// 处理跳跃逻辑
        /// </summary>
        private void HandleJumping(ref Vector3 currentVelocity, float deltaTime)
        {
            // 每帧必要的重置/更新操作 确保每帧都能正确判断
            _jumpedThisFrame = false;
            _timeSinceJumpRequested += deltaTime;


            // 有跳跃请求且可以跳跃
            if (_jumpRequested && CanJump())
            {
                StartJump(ref currentVelocity);
            }

            // 如果正在跳跃中，持续应用跳跃力
            if (_isJumping)
            {
                _jumpElapsedTime += deltaTime;
                ApplyJumpForce(ref currentVelocity);
            }
        }

        /// <summary>
        /// 检查是否可以跳跃
        /// </summary>
        private bool CanJump()
        {
            // 飞扑中可以无限跳跃
            if (_isDashing)
            {
                return true;
            }

            // 通用条件：跳跃次数未达到上限
            if (_currentJumpCount >= MaxJumpCount)
            {
                return false;
            }

            // 满足跳跃次数限制
            bool canJumpFromCountLimit = _currentJumpCount < MaxJumpCount;

            // 输入缓冲：玩家提前按跳跃，在缓冲期内落地也可以跳
            bool hasValidJumpRequest = _jumpRequested && _timeSinceJumpRequested <= JumpBufferTime;

            return canJumpFromCountLimit && hasValidJumpRequest;
        }

        /// <summary>
        /// 开始跳跃
        /// </summary>
        private void StartJump(ref Vector3 currentVelocity)
        {
            // 强制离地，防止被地面吸附
            Motor.ForceUnground(0.1f);

            // 跳跃打断飞扑
            if (_isDashing)
            {
                _isDashing = false;
                _dashVelocity = Vector3.zero;
            }

            // 设置跳跃状态
            _jumpElapsedTime = 0f; // 重置累计时间
            _currentJumpCount++;

            // 消耗跳跃次数，获得飞扑次数
            _availableDashCount++;

            _isJumping = true;
            _jumpRequested = false;
            _jumpedThisFrame = true;
        }

        /// <summary>
        /// 应用跳跃力（基于曲线和时间）
        /// </summary>
        private void ApplyJumpForce(ref Vector3 currentVelocity)
        {

            // 计算跳跃时间进度（0到1），使用累计时间而不是Time.time

            float jumpProgress = _jumpElapsedTime / JumpDuration;

            // 如果跳跃时间超过持续时间，结束跳跃
            if (jumpProgress >= 1f)
            {
                _isJumping = false;
                return;
            }

            // 从曲线获取速度倍率（如果没有曲线，使用1.0）
            float speedMultiplier = JumpSpeedCurve != null ? JumpSpeedCurve.Evaluate(jumpProgress) : 1f;

            // 移除当前垂直速度分量


            currentVelocity -= Vector3.Project(currentVelocity, Motor.CharacterUp);

            // 计算并应用跳跃速度
            Vector3 jumpVelocity = Motor.CharacterUp * JumpForce * speedMultiplier;
            currentVelocity += jumpVelocity;
        }

        /// <summary>
        /// 处理跳跃状态管理
        /// </summary>
        private void HandleJumpStateManagement(float deltaTime)
        {
            // 如果跳跃请求超过缓冲时间，清除请求
            if (_jumpRequested && _timeSinceJumpRequested > JumpBufferTime)
            {
                _jumpRequested = false;
            }

            // 检查是否在地面上
            bool isOnGround = Motor.GroundingStatus.IsStableOnGround;

            if (isOnGround && !_jumpedThisFrame)
            {
                // 落地时完整重置跳跃状态（正常结束跳跃周期）
                _currentJumpCount = 0;
                _isJumping = false;
                _jumpElapsedTime = 0f;

                // 落地重置飞扑次数（仅当不在飞扑状态时）
                if (!_isDashing)
                {
                    _availableDashCount = 0;
                }
            }
        }

        public void AfterCharacterUpdate(float deltaTime)
        {
            // 处理跳跃状态管理
            HandleJumpStateManagement(deltaTime);
        }
        #endregion

        #region 运动器接口 - 碰撞检测
        public bool IsColliderValidForCollisions(Collider coll)
        {
            return true;
        }

        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
            // 预留：着陆逻辑
        }

        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
            // 检测是否撞到头顶（法线方向与角色上方向相反）
            // 点积 > 0 表示法线有向下的分量，说明撞到天花板或倾斜表面
            float dotProduct = Vector3.Dot(hitNormal, -Motor.CharacterUp);
            if (dotProduct > 0.7f && _isJumping)
            {
                // 撞到头顶时停止跳跃状态
                _isJumping = false;
            }
        }

        public void PostGroundingUpdate(float deltaTime)
        {
            // 预留：地面相关后处理
        }

        public void AddVelocity(Vector3 velocity)
        {
            // 预留：外力影响逻辑
        }

        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
            // 预留：修改碰撞稳定性判断
        }

        public void OnDiscreteCollisionDetected(Collider hitCollider)
        {
            // 预留：特殊碰撞检测处理
        }
        #endregion
    }
}

