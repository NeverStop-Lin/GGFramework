using UnityEngine;
using Zenject;
using Framework.Core.Attributes;

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
    [Header("跟随设置")]
    [Label("跟随目标", "相机位置围绕此目标做轨道运动（必需）")]
    public GameObject followTarget;

    [Label("注视目标", "相机朝向注视的目标（可选，为空时与跟随目标相同）")]
    public GameObject lookAtTarget;

    [Label("跟随距离", "相机与跟随目标的固定距离")]
    public float distance = 10f;

    [Header("轨道控制")]
    [Label("俯仰角下限", "最小俯仰角（度），限制相机不会过度低头")]
    public float minPitchDeg = -30f;

    [Label("俯仰角上限", "最大俯仰角（度），限制相机不会过度抬头")]
    public float maxPitchDeg = 80f;

    [Label("水平灵敏度", "手动控制时的水平旋转灵敏度")]
    public float orbitSensitivityYaw = 1f;

    [Label("垂直灵敏度", "手动控制时的垂直旋转灵敏度")]
    public float orbitSensitivityPitch = 1f;

    [Header("平滑设置")]
    [Label("角度平滑时间", "角度过渡的平滑时间（秒），越小响应越快")]
    public float rotationSmoothTime = 0.1f;

    [Label("位置平滑时间", "位置过渡的平滑时间（秒），越小响应越快")]
    public float positionSmoothTime = 0.1f;

    [Label("自动跟随速度", "自动模式下的角度追赶速度（度/秒）")]
    public float autoFollowSpeed = 180f;
    #endregion

    #region 手动模式状态
    // 当前累积的轨道角度（度）
    private float yawDeg;
    private float pitchDeg;

    // 是否已根据初始位置初始化角度
    private bool isOrbitInitialized;

    // 平滑插值速度变量
    private float yawVelocity;
    private float pitchVelocity;
    private Vector3 positionVelocity;
    #endregion

    #region 输入队列
    // 本帧是否有外部输入
    private bool hasQueuedInput;

    // 队列化的角度增量（度）

    private float queuedYawDelta;
    private float queuedPitchDelta;
    #endregion

    #region 自动模式状态
    // 是否已捕获构图（进入自动模式时捕获）
    private bool isCompositionCaptured;

    // 捕获的水平环绕半径（忽略Y轴的距离）

    private float capturedHorizontalRadius;

    // 捕获的相对高度（相机Y - 目标Y）

    private float capturedRelativeHeight;
    #endregion



    #region Unity 生命周期
    private void LateUpdate()
    {
        // 验证目标有效性
        ValidateTarget();

        // 缓存目标位置（避免重复访问 transform.position）
        Vector3 followTargetPos = followTarget.transform.position;
        Vector3 lookAtTargetPos = lookAtTarget != null 
            ? lookAtTarget.transform.position 
            : followTargetPos;

        // 确保轨道角度已初始化
        EnsureOrbitInitialized(followTargetPos);

        // 确定本帧的输入来源（手动 or 自动）
        (float yawDelta, float pitchDelta, bool isAutoMode) = DetermineFrameInput(followTargetPos);

        // 应用旋转（带平滑插值）
        ApplyRotation(yawDelta, pitchDelta);

        // 更新相机位置（带平滑插值）
        UpdateCameraPosition(isAutoMode, followTargetPos, lookAtTargetPos);

        // 清空输入队列
        ClearInputQueue();
    }
    #endregion

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

        queuedYawDelta += yawDelta;
        queuedPitchDelta += pitchDelta;
        hasQueuedInput = true;
    }

    /// <summary>
    /// 清空已队列的输入增量
    /// </summary>
    public void ClearYawPitchInput()
    {
        hasQueuedInput = false;
        queuedYawDelta = 0f;
        queuedPitchDelta = 0f;
    }
    #endregion

    #region 核心逻辑
    /// <summary>
    /// 验证目标存在，缺失时立即抛出异常
    /// </summary>
    private void ValidateTarget()
    {
        if (followTarget == null)
        {
            throw new System.InvalidOperationException(
                $"CameraFollow on '{gameObject.name}' requires a non-null followTarget."
            );
        }
    }

    /// <summary>
    /// 确定本帧的输入来源：手动输入 or 自动跟随
    /// 返回：(yawDelta, pitchDelta, isAutoMode)
    /// </summary>
    private (float, float, bool) DetermineFrameInput(Vector3 followTargetPos)
    {
        if (hasQueuedInput)
        {
            // 有手动输入：立即切换到手动模式
            // 重置自动模式的构图捕获状态
            isCompositionCaptured = false;
            return (queuedYawDelta, queuedPitchDelta, false);
        }
        else
        {
            // 无手动输入：进入自动跟随模式
            (float yawDelta, float pitchDelta) = ComputeAutoFollowAngles(followTargetPos);
            return (yawDelta, pitchDelta, true);
        }
    }

    /// <summary>
    /// 计算自动跟随模式下的角度增量
    /// 目标：维持玩家最后一次手动设定的构图（水平半径 + 相对高度）
    /// </summary>
    private (float, float) ComputeAutoFollowAngles(Vector3 followTargetPos)
    {
        // 首次进入自动模式：捕获当前构图
        if (!isCompositionCaptured)
        {
            Vector3 horizontalOffset = transform.position - followTargetPos;
            horizontalOffset.y = 0f;

            float flatRadius = horizontalOffset.magnitude;
            capturedHorizontalRadius = Mathf.Max(0.0001f, flatRadius);
            capturedRelativeHeight = transform.position.y - followTargetPos.y;
            isCompositionCaptured = true;
        }

        // 计算期望位置：基于当前水平方向 + 捕获的半径/高度
        Vector3 currentHorizontal = transform.position - followTargetPos;
        currentHorizontal.y = 0f;

        float currentHorizontalSqrMag = currentHorizontal.sqrMagnitude;
        if (currentHorizontalSqrMag < 0.0001f)
        {
            currentHorizontal = Vector3.back;
            currentHorizontalSqrMag = 1f;
        }

        // 优化：复用归一化结果
        Vector3 currentHorizontalNormalized = currentHorizontal / Mathf.Sqrt(currentHorizontalSqrMag);
        Vector3 desiredPosition = followTargetPos
            + currentHorizontalNormalized * capturedHorizontalRadius
            + Vector3.up * capturedRelativeHeight;

        // 从期望位置反解 yaw/pitch
        Vector3 offset = desiredPosition - followTargetPos;
        float offsetMag = offset.magnitude;
        if (offsetMag < 0.0001f)
        {
            return (0f, 0f);
        }

        // 优化：直接计算而不是先 normalize 再分别计算
        float invOffsetMag = 1f / offsetMag;
        float dirX = offset.x * invOffsetMag;
        float dirY = offset.y * invOffsetMag;
        float dirZ = offset.z * invOffsetMag;

        float desiredYaw = Mathf.Atan2(-dirX, -dirZ) * Mathf.Rad2Deg;

        // 优化：复用 dirX 和 dirZ 的平方计算水平长度
        float horizontalSqrLength = dirX * dirX + dirZ * dirZ;
        float horizontalLength = Mathf.Sqrt(horizontalSqrLength);
        float desiredPitch = Mathf.Atan2(dirY, horizontalLength) * Mathf.Rad2Deg;
        desiredPitch = Mathf.Clamp(desiredPitch, minPitchDeg, maxPitchDeg);

        // 转换为角度增量（最短路径）
        float yawDelta = Mathf.DeltaAngle(yawDeg, desiredYaw);
        float pitchDelta = Mathf.DeltaAngle(pitchDeg, desiredPitch);

        // 帧率独立：限制每帧旋转速度
        float maxDeltaThisFrame = autoFollowSpeed * Time.deltaTime;
        yawDelta = Mathf.Clamp(yawDelta, -maxDeltaThisFrame, maxDeltaThisFrame);
        pitchDelta = Mathf.Clamp(pitchDelta, -maxDeltaThisFrame, maxDeltaThisFrame);

        return (yawDelta, pitchDelta);
    }

    /// <summary>
    /// 应用旋转：累积角度并限制范围（带平滑插值）
    /// </summary>
    private void ApplyRotation(float yawDelta, float pitchDelta)
    {
        // 计算目标角度，应用灵敏度
        float targetYaw = yawDeg + yawDelta * orbitSensitivityYaw;
        float targetPitch = pitchDeg + pitchDelta * orbitSensitivityPitch;

        // 限制俯仰角范围
        targetPitch = Mathf.Clamp(targetPitch, minPitchDeg, maxPitchDeg);

        // 平滑插值到目标角度
        yawDeg = Mathf.SmoothDampAngle(yawDeg, targetYaw, ref yawVelocity, rotationSmoothTime);
        pitchDeg = Mathf.SmoothDampAngle(pitchDeg, targetPitch, ref pitchVelocity, rotationSmoothTime);
    }

    /// <summary>
    /// 更新相机位置：基于轨道角度和距离（带平滑插值和极端角度保护）
    /// </summary>
    private void UpdateCameraPosition(bool isAutoMode, Vector3 followTargetPos, Vector3 lookAtTargetPos)
    {
        float usedDistance = Mathf.Max(0.0001f, distance);

        // 自动模式特殊处理：动态调整距离以保持水平半径不变
        if (isAutoMode)
        {
            float cosPitch = Mathf.Cos(pitchDeg * Mathf.Deg2Rad);
            cosPitch = Mathf.Abs(cosPitch);
            
            // 极端角度保护：当 pitch 接近 ±90° 时，限制最大距离
            if (cosPitch < 0.1f)
            {
                // pitch 接近垂直，使用固定最大距离避免无限放大
                usedDistance = Mathf.Min(distance * 3f, capturedHorizontalRadius / 0.1f);
            }
            else
            {
                usedDistance = capturedHorizontalRadius / cosPitch;
                // 进一步限制防止异常值
                usedDistance = Mathf.Clamp(usedDistance, distance * 0.5f, distance * 3f);
            }
        }

        // 计算目标位置
        Vector3 offset = Quaternion.Euler(pitchDeg, yawDeg, 0f) * Vector3.back * usedDistance;
        Vector3 targetPosition = followTargetPos + offset;

        // 平滑插值到目标位置
        transform.position = Vector3.SmoothDamp(
            transform.position, 
            targetPosition, 
            ref positionVelocity, 
            positionSmoothTime
        );

        // 注视目标（使用分离的 lookAt 目标）
        transform.LookAt(lookAtTargetPos);
    }

    /// <summary>
    /// 清空本帧的输入队列
    /// </summary>
    private void ClearInputQueue()
    {
        hasQueuedInput = false;
        queuedYawDelta = 0f;
        queuedPitchDelta = 0f;
    }
    #endregion

    #region 初始化
    /// <summary>
    /// 初始化轨道角度：基于当前相机与目标的相对向量
    /// 避免首次启用时发生画面跳变
    /// </summary>
    private void EnsureOrbitInitialized(Vector3 followTargetPos)
    {
        if (isOrbitInitialized)
        {
            return;
        }

        Vector3 offset = transform.position - followTargetPos;

        if (offset.sqrMagnitude < 0.0001f)
        {
            // 若几乎重合，给出一个稳定偏移
            offset = Vector3.back * Mathf.Max(0.0001f, distance);
        }

        // 由偏移反解 yaw/pitch（度）
        // 优化：直接计算避免临时对象
        float offsetMag = offset.magnitude;
        float invOffsetMag = 1f / offsetMag;
        float dirX = offset.x * invOffsetMag;
        float dirY = offset.y * invOffsetMag;
        float dirZ = offset.z * invOffsetMag;

        // Yaw: 绕 Y 轴角度（水平方向）
        yawDeg = Mathf.Atan2(-dirX, -dirZ) * Mathf.Rad2Deg;

        // Pitch: 俯仰角（垂直方向）
        float horizontalSqrLength = dirX * dirX + dirZ * dirZ;
        float horizontalLength = Mathf.Sqrt(horizontalSqrLength);
        pitchDeg = Mathf.Atan2(dirY, horizontalLength) * Mathf.Rad2Deg;
        pitchDeg = Mathf.Clamp(pitchDeg, minPitchDeg, maxPitchDeg);

        isOrbitInitialized = true;
    }
    #endregion
}
