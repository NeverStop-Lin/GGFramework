using UnityEngine;
using KinematicCharacterController;
using GameApp.Character;
using Framework.Core.Attributes;

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
        public Vector3 Gravity = new Vector3(0, -30f, 0);
        #endregion

        #region 私有字段
        private Vector3 _moveInputVector; // 世界坐标系的移动输入
        #endregion

        #region Unity生命周期
        private void Start()
        {
            ValidateComponents();
            RegisterToMotor();
            ApplyConfigValues();
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
        /// 注册控制器到运动器
        /// </summary>
        private void RegisterToMotor()
        {
            Motor.CharacterController = this;
        }

        /// <summary>
        /// 应用配置资源中的参数值
        /// </summary>
        private void ApplyConfigValues()
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
            // 如果移动输入方向无效或平滑度为0，不更新旋转
            if (_moveInputVector.sqrMagnitude <= MIN_SQR_MAGNITUDE_THRESHOLD || RotationSmoothness <= 0f)
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
            Vector3 smoothedDirection = CalculateSmoothedRotationDirection(currentForward, targetDirection, deltaTime);
            currentRotation = Quaternion.LookRotation(smoothedDirection, Motor.CharacterUp);
        }

        /// <summary>
        /// 计算平滑后的旋转方向（仅在水平面）
        /// </summary>
        private Vector3 CalculateSmoothedRotationDirection(Vector3 currentDirection, Vector3 targetDirection, float deltaTime)
        {
            float smoothFactor = 1f - Mathf.Exp(-RotationSmoothness * deltaTime);
            return Vector3.Slerp(currentDirection, targetDirection, smoothFactor).normalized;
        }
        #endregion

        /// <summary>
        /// 更新角色速度（唯一可设置速度的地方）
        /// </summary>
        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (Motor.GroundingStatus.IsStableOnGround)
            {
                HandleStableMovement(ref currentVelocity, deltaTime);
            }
            else
            {
                HandleAirMovement(ref currentVelocity, deltaTime);
            }
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



        public void AfterCharacterUpdate(float deltaTime)
        {
            // 预留：移动后的后处理逻辑
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
            // 预留：碰撞处理逻辑（如墙壁跳等）
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

