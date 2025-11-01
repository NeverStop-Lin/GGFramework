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
    private float _autoFollowRadius; // “记忆”的水平半径 (替代 _autoFollowOffsetProjectionLength)
    private float _autoFollowHeight; // “记忆”的相对高度
    private Vector3 _autoFollowPosition; // 重新加回来！它将存储上一帧的“理想相机世界坐标”
    // ... 其他代码 ...
    private bool _isAutoFollowInitialized = false; // 新增！自动模式初始化标志
    void HandheldAutoFollow()
    {
        // --- Part 1: 初始化/记忆构图 (模式切换或首次运行时) ---
        if (_lastFollowMode != _followMode || !_isAutoFollowInitialized)
        {
            // --- 核心修正 ---
            // 无论如何，只要进入自动模式，就将高度设置为固定的期望值。
            _autoFollowHeight = 4f; // 或者你可以把它做成一个公共变量在Inspector里设置

            if (_lastFollowMode == FollowMode.Manual)
            {
                // 我们仍然“记忆”水平距离，但不再记忆高度
                Vector3 lastOffset = _transposer.m_FollowOffset;
                _autoFollowRadius = new Vector3(lastOffset.x, 0, lastOffset.z).magnitude;
                _autoFollowRadius = Mathf.Min(_autoFollowRadius, distance);
            }
            else
            {
                // 游戏启动时，使用预设的距离
                _autoFollowRadius = distance;
            }

            if (_autoFollowRadius < 0.1f) { _autoFollowRadius = distance; }

            _autoFollowPosition = transform.position;
            _isAutoFollowInitialized = true;
        }

        // --- Part 2: 每帧更新部分 (这部分已是最终形态，无需修改) ---
        Vector3 targetPosition = cinemachineVirtualCamera.Follow.transform.position;
        Vector3 direction = _autoFollowPosition - targetPosition;
        direction.y = 0;
        if (direction.sqrMagnitude < 0.001f)
        {
            direction = -cinemachineVirtualCamera.Follow.transform.forward;
        }
        Vector3 horizontalOffset = direction.normalized * _autoFollowRadius;
        Vector3 finalOffset = new Vector3(horizontalOffset.x, _autoFollowHeight, horizontalOffset.z);
        _autoFollowPosition = targetPosition + finalOffset;
        SetFollowOffset(finalOffset);
    }
    void HandheldEnemyFollow()
    {

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
