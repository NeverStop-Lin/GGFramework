using UnityEngine;
using Zenject;
using Framework.Core.Attributes;
using Cinemachine;
using Framework.Core;

/// <summary>
/// 智能轨道跟随相机系统
/// 
/// 核心特性：
/// - 双模态设计：手动轨道控制 + 自动构图跟随
/// - 无缝切换：玩家输入立即切换到手动模式，松手后自动捕获构图并进入自动模式
/// - 构图记忆：自动模式维持玩家最后一次手动调整的画面（水平半径和相对高度）
/// - 弧线漂移：目标转向时，相机以平滑弧线重新对齐构图，产生电影感
/// </summary>
public class CameraFollow : MonoBehaviour
{
    #region 配置参数
    [Header("必须组件")]
    [Label("相机", "相机组件（必需）")]
    public CinemachineVirtualCamera cinemachineVirtualCamera;


    [Label("跟随距离", "相机与跟随目标的固定距离")]
    public float distance = 10f;

    [Label("敌人目标", "相机朝向注视的敌人（可选，为空时与跟随目标相同）")]
    public GameObject enemyTarget;

    [Label("俯仰角下限", "最小俯仰角（度），限制相机不会过度低头")]
    public float minPitchDeg = 30;

    [Label("俯仰角上限", "最大俯仰角（度），限制相机不会过度抬头")]
    public float maxPitchDeg = 150;

    [Label("水平灵敏度", "手动控制时的水平旋转灵敏度")]
    public float orbitSensitivityYaw = 1f;

    [Label("垂直灵敏度", "手动控制时的垂直旋转灵敏度")]
    public float orbitSensitivityPitch = 1f;

    [Label("水平平滑时间", "手动控制时的水平旋转平滑时间")]
    public float orbitSmoothTimeYaw = 0.1f;

    [Label("垂直平滑时间", "手动控制时的垂直旋转平滑时间")]
    public float orbitSmoothTimePitch = 0.1f;
    #endregion



    #region 输入队列
    // 本帧是否有外部输入
    private bool hasQueuedInput;
    // 队列化的角度增量（度）
    private float queuedYawDelta;
    private float queuedPitchDelta;
    #endregion

    private CinemachineTransposer _transposer;
    // 跟随模式
    // 跟随模式枚举
    public enum FollowMode
    {
        None, // 无模式
        Auto, // 自动模式
        Manual, // 手动模式
        Enemy // 敌人模式
    }

    [ReadOnly][SerializeField] private FollowMode _followMode = FollowMode.None;
    private FollowMode _lastFollowMode = FollowMode.None;
    private float _manualYawDeg = 0f;
    private float _manualPitchDeg = 0f;

    private float _autoFollowOffsetProjectionLength = 0f;
    #region Unity 生命周期
    void Start()
    {
        if (_transposer == null)
        {
            _transposer = cinemachineVirtualCamera.GetCinemachineComponent<CinemachineTransposer>();
        }
        InitializeCameraState();
    }
    private void LateUpdate()
    {

        UpdateFollowMode(); // 更新跟随模式

        switch (_followMode)
        {
            case FollowMode.Auto:
                HandheldAutoFollow();
                break;
            case FollowMode.Manual:
                HandheldManualFollow();
                break;
            case FollowMode.Enemy:
                HandheldEnemyFollow();
                break;
        }
    }
    #endregion

    void SetFollowOffset(Vector3 offset)
    {

        _transposer.m_FollowOffset = offset;
    }

    // 判断状态和更新
    void UpdateFollowMode()
    {
        FollowMode newMode;
        if (enemyTarget != null)
        {
            newMode = FollowMode.Enemy;
        }
        else if (queuedYawDelta != 0f || queuedPitchDelta != 0f)
        {
            newMode = FollowMode.Manual;
        }
        else
        {
            newMode = FollowMode.Auto;
        }

        _lastFollowMode = _followMode;
        _followMode = newMode;
    }
    private float _manualRadius;

    void HandheldManualFollow()
    {
        // 当模式切换时，初始化球坐标
        if (_lastFollowMode != _followMode)
        {
            var spherical = SphericalCoordinates.FromCartesian(_transposer.m_FollowOffset);

            // --- 修正部分 ---
            // 1. 保存初始的半径
            _manualRadius = spherical.radius;

            // 2. 从弧度转换为角度进行存储
            _manualYawDeg = spherical.phi * Mathf.Rad2Deg;
            _manualPitchDeg = spherical.theta * Mathf.Rad2Deg;
        }

        // --- 这部分输入处理逻辑是正确的，无需修改 ---
        if (orbitSmoothTimeYaw <= 0f) orbitSmoothTimeYaw = 0.01f;
        if (orbitSmoothTimePitch <= 0f) orbitSmoothTimePitch = 0.01f;

        float usedYawDelta = 1 / orbitSmoothTimeYaw * queuedYawDelta * Time.deltaTime;
        float usedPitchDelta = 1 / orbitSmoothTimePitch * queuedPitchDelta * Time.deltaTime;
        queuedYawDelta -= usedYawDelta;
        queuedPitchDelta -= usedPitchDelta;

        if (Mathf.Abs(queuedYawDelta) < 1e-5f) queuedYawDelta = 0f;
        if (Mathf.Abs(queuedPitchDelta) < 1e-5f) queuedPitchDelta = 0f;

        _manualYawDeg += usedYawDelta;
        _manualPitchDeg += usedPitchDelta;

        // 限制俯仰角范围 (注意theta是从Y轴向下算的, 0是正上方, 180是正下方)
        // 比如你希望相机在水平线上下45度活动，那么范围大概是 [45, 135]
        _manualPitchDeg = Mathf.Clamp(_manualPitchDeg, 180 - maxPitchDeg, 180 - minPitchDeg);

        // --- 修正部分 ---
        // 1. 创建球坐标时，使用正确的参数顺序：(半径, 极角, 方位角)
        // 2. 将角度从 Degrees 转换为 Radians
        Spherical sphericalOffset = new Spherical(
            _manualRadius,                              // 参数1: 半径
            _manualPitchDeg * Mathf.Deg2Rad,            // 参数2: 极角 (theta)，并转为弧度
            _manualYawDeg * Mathf.Deg2Rad               // 参数3: 方位角 (phi)，并转为弧度
        );

        // 从修正后的球坐标转换回笛卡尔坐标
        Vector3 offset = SphericalCoordinates.ToCartesian(sphericalOffset);

        SetFollowOffset(offset);

        // 更新 _lastFollowMode 的逻辑应该放在函数末尾
        _lastFollowMode = _followMode;
    }

    // 在你的类成员变量区域，用这两个新变量替换掉 _autoFollowPosition 和 _autoFollowOffsetProjectionLength
    [Header("自动跟随模式配置")]
    [Tooltip("自动跟随模式下，相机与目标的固定3D空间距离。")]
    public float fixedDistance = 10f;

    [Tooltip("自动跟随模式下，相机与目标的固定相对高度。")]
    public float fixedHeight = 4f;

    private bool _isAutoFollowInitialized = false;
    private float _autoFollowHorizontalRadius; // 【关键】由上面两个值计算出的固定水平半径
    private Vector3 _autoFollowPosition;
    void HandheldAutoFollow()
    {
        // --- Part 1: 初始化/记忆构图 ---
        if (_lastFollowMode != _followMode || !_isAutoFollowInitialized)
        {
            // 1. 【核心】根据勾股定理，计算出固定的水平半径
            //    安全校验，确保总距离大于高度，否则无法构成三角形
            if (fixedDistance < fixedHeight)
            {
                Debug.LogError("相机配置错误：fixedDistance 必须大于 fixedHeight！");
                // 使用一个安全的回退值
                _autoFollowHorizontalRadius = fixedDistance;
            }
            else
            {
                _autoFollowHorizontalRadius = Mathf.Sqrt(fixedDistance * fixedDistance - fixedHeight * fixedHeight);
            }

            // 2. 初始化漂移位置，防止镜头跳跃
            _autoFollowPosition = transform.position;

            _isAutoFollowInitialized = true;
        }

        // --- Part 2: 每帧更新/维持构图 ---
        Vector3 targetPosition = cinemachineVirtualCamera.Follow.transform.position;

        // 1. 【核心】在水平面上计算漂移方向
        //    从当前目标位置，指向上一帧的理想相机位置，但只考虑水平投影
        Vector3 direction = _autoFollowPosition - targetPosition;
        direction.y = 0; // 强制在X-Z平面上进行漂移

        if (direction.sqrMagnitude < 0.001f)
        {
            // 如果方向向量过小，使用一个默认的后方方向
            direction = -cinemachineVirtualCamera.Follow.transform.forward;
            direction.y = 0; // 确保是水平的
        }
        direction.Normalize();

        // 2. 计算理想的【水平】偏移量
        //    使用“漂移后”的水平方向，乘以我们计算出的固定水平半径
        Vector3 horizontalOffset = direction * _autoFollowHorizontalRadius;

        // 3. 构建最终的3D偏移向量
        //    结合固定的水平偏移和固定的垂直高度
        Vector3 idealOffset = new Vector3(horizontalOffset.x, fixedHeight, horizontalOffset.z);

        // 4. 更新状态，为下一帧做准备
        _autoFollowPosition = targetPosition + idealOffset;

        // 5. 将最终的理想偏移量交给Cinemachine
        SmoothlyUpdateFollowOffset(idealOffset);
    }

    // 在您的类成员变量区域，添加这两个新变量

    [Header("自定义平滑配置")]
    [Tooltip("相机跟随的平滑时间。值越大，相机越'懒'、越平滑。推荐值 0.1 ~ 0.5")]
    public float customSmoothTime = 0.25f;

    // 这个变量是 SmoothDamp 函数内部需要的，用来存储当前速度，我们不需要手动修改它
    private Vector3 _cameraOffsetVelocity = Vector3.zero;

    /// <summary>
    /// 使用 SmoothDamp 平滑地更新相机的 Follow Offset。
    /// </summary>
    /// <param name="idealOffset">我们希望相机最终到达的理想偏移位置</param>
    void SmoothlyUpdateFollowOffset(Vector3 idealOffset)
    {
        // 1. 获取相机当前的“真实”偏移量
        Vector3 currentOffset = _transposer.m_FollowOffset;

        // 2. 使用 SmoothDamp 计算出下一帧的平滑位置
        //    这个函数会从 currentOffset 向 idealOffset 平滑地移动，
        //    大约在 customSmoothTime 秒内完成大部分移动。
        Vector3 smoothedOffset = Vector3.SmoothDamp(
            currentOffset,          // 当前位置
            idealOffset,            // 目标位置
            ref _cameraOffsetVelocity, // 当前速度（函数会自动更新它）
            customSmoothTime        // 到达目标所需的大致时间
        );

        // 3. 将我们自己计算出的“平滑后”的偏移量，设置给 Cinemachine
        SetFollowOffset(smoothedOffset);
    }
    void HandheldEnemyFollow()
    {

    }

    /// <summary>
    /// 在游戏开始时调用，用于计算并立即设置相机的初始位置和状态，防止镜头跳跃。
    /// </summary>
    void InitializeCameraState()
    {
        // --- 安全校验 ---
        if (cinemachineVirtualCamera == null || cinemachineVirtualCamera.Follow == null)
        {
            Debug.LogError("相机或跟随目标未设置，无法初始化相机位置！");
            return;
        }

        // --- 1. 计算固定的水平半径 (与HandheldAutoFollow中的逻辑相同) ---
        if (fixedDistance < fixedHeight)
        {
            Debug.LogError("相机配置错误：fixedDistance 必须大于 fixedHeight！");
            _autoFollowHorizontalRadius = fixedDistance;
        }
        else
        {
            _autoFollowHorizontalRadius = Mathf.Sqrt(fixedDistance * fixedDistance - fixedHeight * fixedHeight);
        }

        // --- 2. 计算初始的理想偏移量 ---
        // 在游戏开始时，我们给一个默认的、在目标正后方的方向
        Vector3 initialDirection = -cinemachineVirtualCamera.Follow.transform.forward;
        initialDirection.y = 0;
        initialDirection.Normalize();

        Vector3 horizontalOffset = initialDirection * _autoFollowHorizontalRadius;
        Vector3 initialOffset = new Vector3(horizontalOffset.x, fixedHeight, horizontalOffset.z);

        // --- 3. 立即设置位置和状态 (核心步骤) ---
        Vector3 targetPosition = cinemachineVirtualCamera.Follow.transform.position;
        Vector3 initialWorldPosition = targetPosition + initialOffset;

        // a. 立即将相机传送到目标位置，消除视觉跳跃
        transform.position = initialWorldPosition;

        // b. 立即更新Cinemachine的偏移量，保持内部状态一致
        SetFollowOffset(initialOffset);

        // c. 初始化漂移逻辑的“上一帧位置”，防止第一帧更新时出错
        _autoFollowPosition = initialWorldPosition;

        // d. 标记为已初始化
        _isAutoFollowInitialized = true;
    }

    #region 公共接口
    /// <summary>
    /// 队列化本帧的 yaw/pitch 增量（单位：度）
    /// 会在 LateUpdate 中被消费，并立即切换到手动模式
    /// </summary>
    public void QueueYawPitchInput(float yawDelta, float pitchDelta)
    {
        // 参数校验（Fail Fast）
        if (float.IsNaN(yawDelta) || float.IsInfinity(yawDelta))
        {
            throw new System.ArgumentException($"Invalid yawDelta: {yawDelta}");
        }
        if (float.IsNaN(pitchDelta) || float.IsInfinity(pitchDelta))
        {
            throw new System.ArgumentException($"Invalid pitchDelta: {pitchDelta}");
        }

        queuedYawDelta += yawDelta * orbitSensitivityYaw;
        queuedPitchDelta += pitchDelta * orbitSensitivityPitch;
        hasQueuedInput = true;

    }

    // /// <summary>
    // /// 清空已队列的输入增量
    // /// </summary>
    // public void ClearYawPitchInput()
    // {
    //     hasQueuedInput = false;
    //     queuedYawDelta = 0f;
    //     queuedPitchDelta = 0f;
    // }
    #endregion


}
