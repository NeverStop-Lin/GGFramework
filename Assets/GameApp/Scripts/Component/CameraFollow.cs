using UnityEngine;
using Zenject;
using Framework.Core.Attributes;
using Cinemachine;
using Framework.Core;

/// <summary>
/// 智能轨道跟随相机系统
/// 支持三种跟随模式：手动轨道控制、自动构图跟随、锁敌模式
/// </summary>
public class CameraFollow : MonoBehaviour
{
    #region 配置参数

    [Header("相机组件（必需）")]
    public CinemachineVirtualCamera cinemachineVirtualCamera;
    [Header("手动模式参数")]
    public ManualModeSettings manualModeSettings = new ManualModeSettings();
    [Header("自动模式参数")]
    public AutoModeSettings autoModeSettings = new AutoModeSettings();
    [Header("锁敌模式参数")]
    public EnemyModeSettings enemyModeSettings = new EnemyModeSettings();

    #endregion

    #region 配置结构体
    [System.Serializable]
    public class ManualModeSettings
    {
        [Label("俯仰角下限", "最小俯仰角（度） 推荐值 80")]
        public float minPitchDeg = 80;

        [Label("俯仰角上限", "最大俯仰角（度） 推荐值 150")]
        public float maxPitchDeg = 150;

        [Label("水平灵敏度", "水平旋转灵敏度 推荐值 0.1")]
        public float orbitSensitivityYaw = 0.1f;

        [Label("垂直灵敏度", "垂直旋转灵敏度 推荐值 0.1")]
        public float orbitSensitivityPitch = 0.1f;

        [Label("水平平滑时间", "水平旋转平滑时间 推荐值 0.1")]
        public float orbitSmoothTimeYaw = 0.1f;

        [Label("垂直平滑时间", "垂直旋转平滑时间 推荐值 0.1")]
        public float orbitSmoothTimePitch = 0.1f;
    }

    [System.Serializable]
    public class AutoModeSettings
    {
        [Label("相机与目标的固定3D空间距离 推荐值 10")]
        public float fixedDistance = 10f;

        [Label("相机与目标的固定相对高度 推荐值 4")]
        public float fixedHeight = 4f;

        [Label("相机跟随的平滑时间（推荐值 0.1 ~ 0.2）")]
        public float autoFollowSmoothTime = 0.2f;
    }

    [System.Serializable]
    public class EnemyModeSettings
    {
        [Label("敌人目标（可选）")]
        public GameObject enemyTarget;

        [Label("触发锁敌的最小距离 推荐值 3")]
        public float minEnemyLockDistance = 3f;

        [Label("相机跟随的平滑时间（推荐值 0.5）")]
        public float enemyFollowSmoothTime = 0.5f;
    }
    #endregion

    #region Cinemachine 组件
    private CinemachineTransposer _transposer;
    #endregion

    #region 模式状态
    public enum FollowMode
    {
        None,
        Auto,
        Manual,
        Enemy
    }
    [Header("调试信息")][ReadOnly][SerializeField][Label("当前跟随模式", "", true)] private FollowMode _followMode = FollowMode.None;
    private FollowMode _lastFollowMode = FollowMode.None;
    #endregion

    #region 输入队列
    private float queuedYawDelta;
    private float queuedPitchDelta;
    #endregion

    #region 手动模式状态
    private float _manualYawDeg = 0f;
    private float _manualPitchDeg = 0f;
    private float _manualRadius;
    #endregion

    #region 自动模式状态
    private bool _isAutoFollowInitialized = false;
    private float _autoFollowHorizontalRadius;
    private Vector3 _autoFollowPosition;
    private Vector3 _cameraOffsetVelocity = Vector3.zero;
    #endregion

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
        UpdateFollowMode();

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

    void UpdateFollowMode()
    {
        bool shouldLockEnemy = false;
        if (enemyModeSettings.enemyTarget != null)
        {
            Vector3 playerPos = cinemachineVirtualCamera.Follow.transform.position;
            Vector3 enemyPos = enemyModeSettings.enemyTarget.transform.position;
            float distanceToEnemy = Vector3.Distance(new Vector3(playerPos.x, 0, playerPos.z), new Vector3(enemyPos.x, 0, enemyPos.z));

            if (distanceToEnemy > enemyModeSettings.minEnemyLockDistance)
            {
                shouldLockEnemy = true;
            }
        }

        FollowMode newMode;
        if (enemyModeSettings.enemyTarget != null && shouldLockEnemy)
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

    void HandheldManualFollow()
    {
        if (_lastFollowMode != _followMode)
        {
            var spherical = SphericalCoordinates.FromCartesian(_transposer.m_FollowOffset);
            _manualRadius = spherical.radius;
            _manualYawDeg = spherical.phi * Mathf.Rad2Deg;
            _manualPitchDeg = spherical.theta * Mathf.Rad2Deg;
        }

        if (manualModeSettings.orbitSmoothTimeYaw <= 0f) manualModeSettings.orbitSmoothTimeYaw = 0.01f;
        if (manualModeSettings.orbitSmoothTimePitch <= 0f) manualModeSettings.orbitSmoothTimePitch = 0.01f;

        float usedYawDelta = 1 / manualModeSettings.orbitSmoothTimeYaw * queuedYawDelta * Time.deltaTime;
        float usedPitchDelta = 1 / manualModeSettings.orbitSmoothTimePitch * queuedPitchDelta * Time.deltaTime;
        queuedYawDelta -= usedYawDelta;
        queuedPitchDelta -= usedPitchDelta;

        if (Mathf.Abs(queuedYawDelta) < 1e-5f) queuedYawDelta = 0f;
        if (Mathf.Abs(queuedPitchDelta) < 1e-5f) queuedPitchDelta = 0f;

        _manualYawDeg += usedYawDelta;
        _manualPitchDeg += usedPitchDelta;

        // 限制俯仰角范围（theta从Y轴向下算，0是正上方，180是正下方）
        _manualPitchDeg = Mathf.Clamp(_manualPitchDeg, 180 - manualModeSettings.maxPitchDeg, 180 - manualModeSettings.minPitchDeg);

        Spherical sphericalOffset = new Spherical(
            _manualRadius,
            _manualPitchDeg * Mathf.Deg2Rad,
            _manualYawDeg * Mathf.Deg2Rad
        );

        Vector3 offset = SphericalCoordinates.ToCartesian(sphericalOffset);
        SetFollowOffset(offset);
        _lastFollowMode = _followMode;
    }

    void HandheldAutoFollow()
    {
        if (_lastFollowMode != _followMode || !_isAutoFollowInitialized)
        {
            // 根据勾股定理计算固定水平半径
            if (autoModeSettings.fixedDistance < autoModeSettings.fixedHeight)
            {
                Debug.LogError("相机配置错误：fixedDistance 必须大于 fixedHeight！");
                _autoFollowHorizontalRadius = autoModeSettings.fixedDistance;
            }
            else
            {
                _autoFollowHorizontalRadius = Mathf.Sqrt(autoModeSettings.fixedDistance * autoModeSettings.fixedDistance - autoModeSettings.fixedHeight * autoModeSettings.fixedHeight);
            }

            _autoFollowPosition = transform.position;
            _isAutoFollowInitialized = true;
        }

        Vector3 targetPosition = cinemachineVirtualCamera.Follow.transform.position;

        // 计算漂移方向（水平面）
        Vector3 direction = _autoFollowPosition - targetPosition;
        direction.y = 0;

        if (direction.sqrMagnitude < 0.001f)
        {
            direction = -cinemachineVirtualCamera.Follow.transform.forward;
            direction.y = 0;
        }
        direction.Normalize();

        Vector3 horizontalOffset = direction * _autoFollowHorizontalRadius;
        Vector3 idealOffset = new Vector3(horizontalOffset.x, autoModeSettings.fixedHeight, horizontalOffset.z);

        _autoFollowPosition = targetPosition + idealOffset;
        SmoothlyUpdateFollowOffset(idealOffset, autoModeSettings.autoFollowSmoothTime);
    }

    void SmoothlyUpdateFollowOffset(Vector3 idealOffset, float smoothTime)
    {
        Vector3 currentOffset = _transposer.m_FollowOffset;
        Vector3 smoothedOffset = Vector3.SmoothDamp(
            currentOffset,
            idealOffset,
            ref _cameraOffsetVelocity,
            smoothTime
        );
        SetFollowOffset(smoothedOffset);
    }
    void HandheldEnemyFollow()
    {
        if (enemyModeSettings.enemyTarget == null || cinemachineVirtualCamera.Follow == null)
        {
            HandheldAutoFollow();
            return;
        }

        Vector3 playerPosition = cinemachineVirtualCamera.Follow.transform.position;
        Vector3 enemyPosition = enemyModeSettings.enemyTarget.transform.position;

        Vector3 directionToEnemy = enemyPosition - playerPosition;
        directionToEnemy.y = 0;

        if (directionToEnemy.sqrMagnitude < 0.001f)
        {
            HandheldAutoFollow();
            return;
        }
        directionToEnemy.Normalize();

        // 相机位于玩家后方，朝向敌人
        Vector3 cameraHorizontalDirection = -directionToEnemy;
        Vector3 horizontalOffset = cameraHorizontalDirection * _autoFollowHorizontalRadius;
        Vector3 idealOffset = new Vector3(horizontalOffset.x, autoModeSettings.fixedHeight, horizontalOffset.z);

        _autoFollowPosition = playerPosition + idealOffset;
        SmoothlyUpdateFollowOffset(idealOffset, enemyModeSettings.enemyFollowSmoothTime);
    }

    void InitializeCameraState()
    {
        if (cinemachineVirtualCamera == null || cinemachineVirtualCamera.Follow == null)
        {
            Debug.LogError("相机或跟随目标未设置，无法初始化相机位置！");
            return;
        }

        // 计算固定水平半径
        if (autoModeSettings.fixedDistance < autoModeSettings.fixedHeight)
        {
            Debug.LogError("相机配置错误：fixedDistance 必须大于 fixedHeight！");
            _autoFollowHorizontalRadius = autoModeSettings.fixedDistance;
        }
        else
        {
            _autoFollowHorizontalRadius = Mathf.Sqrt(autoModeSettings.fixedDistance * autoModeSettings.fixedDistance - autoModeSettings.fixedHeight * autoModeSettings.fixedHeight);
        }

        // 计算初始偏移量（目标正后方）
        Vector3 initialDirection = -cinemachineVirtualCamera.Follow.transform.forward;
        initialDirection.y = 0;
        initialDirection.Normalize();

        Vector3 horizontalOffset = initialDirection * _autoFollowHorizontalRadius;
        Vector3 initialOffset = new Vector3(horizontalOffset.x, autoModeSettings.fixedHeight, horizontalOffset.z);

        // 立即设置相机位置，防止镜头跳跃
        Vector3 targetPosition = cinemachineVirtualCamera.Follow.transform.position;
        Vector3 initialWorldPosition = targetPosition + initialOffset;

        transform.position = initialWorldPosition;
        SetFollowOffset(initialOffset);
        _autoFollowPosition = initialWorldPosition;
        _isAutoFollowInitialized = true;
    }

    #region 公共接口
    /// <summary>
    /// 队列化本帧的 yaw/pitch 增量（单位：度），将在 LateUpdate 中被消费并切换到手动模式
    /// </summary>
    public void QueueYawPitchInput(float yawDelta, float pitchDelta)
    {
        if (float.IsNaN(yawDelta) || float.IsInfinity(yawDelta))
        {
            throw new System.ArgumentException($"Invalid yawDelta: {yawDelta}");
        }
        if (float.IsNaN(pitchDelta) || float.IsInfinity(pitchDelta))
        {
            throw new System.ArgumentException($"Invalid pitchDelta: {pitchDelta}");
        }

        queuedYawDelta += yawDelta * manualModeSettings.orbitSensitivityYaw;
        queuedPitchDelta += pitchDelta * manualModeSettings.orbitSensitivityPitch;
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
