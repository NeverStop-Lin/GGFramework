using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class CameraFollow : MonoBehaviour
{
    // 跟随目标（必需）
    public GameObject target;
    // 期望和目标的距离（保持不变）
    public float distance = 10f;
    // 轨道旋转的俯仰角范围（度）
    public float minPitchDeg = -30f;
    public float maxPitchDeg = 80f;
    // 轨道旋转灵敏度（外部传入增量的缩放）
    public float orbitSensitivityYaw = 1f;
    public float orbitSensitivityPitch = 1f;

    // 内部状态：累计的 yaw/pitch 角（度）
    private float yawDeg;
    private float pitchDeg;
    // 内部状态：本帧是否收到外部旋转输入
    private bool hasQueuedInput;
    private float queuedYawDelta;
    private float queuedPitchDelta;
    // 是否已根据当前相机-目标向量初始化过角度
    private bool orbitInitialized;
    // 内部辅助：自动跟随时用于对齐的虚拟枢轴（仅运行时使用）
    private Transform autoFollowPivot;
    // 继承自由模式的约束：水平半径与相对高度（相对目标）
    private float preservedFlatRadius;
    private float preservedRelativeY;
    private bool autoConstraintsCaptured;
    // Start is called before the first frame update

    [Inject] GamePlayInput gamePlayInput;
    void Start()
    {
        // 运行时创建内部辅助节点（仅作为计算参照物，不依赖外部）
        GameObject pivot = new GameObject("CameraFollow_AutoPivot");
        autoFollowPivot = pivot.transform;
        autoFollowPivot.SetPositionAndRotation(transform.position, transform.rotation);
    }

    private void Update()
    {
        if (gamePlayInput.IsTouchCameraRotateArea)
        {
            QueueYawPitchInput(gamePlayInput.CameraRotateInput.x, gamePlayInput.CameraRotateInput.y);
        }
    }


    // 外部方法：队列化本帧的 yaw/pitch 增量（单位：度），会在 LateUpdate 中被消费
    // 说明：建议由外部把输入（例如鼠标/手柄）换算为每帧的角度增量后调用本方法
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

    // 外部方法：清空已队列的增量
    public void ClearYawPitchInput()
    {
        hasQueuedInput = false;
        queuedYawDelta = 0f;
        queuedPitchDelta = 0f;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (target == null)
        {
            // 缺失必需对象，立即抛出（包含上下文信息）
            throw new System.InvalidOperationException($"CameraFollow on '{gameObject.name}' requires a non-null target.");
        }

        EnsureOrbitInitialized();

        // 统一输入通道：优先消费外部输入；若无外部输入，则根据“跟随模式”自动生成输入
        float consumedYawDelta = 0f;
        float consumedPitchDelta = 0f;
        bool usedAutoInput = false;

        if (hasQueuedInput)
        {
            consumedYawDelta = queuedYawDelta;
            consumedPitchDelta = queuedPitchDelta;
            // 收到外部输入，重置自动约束捕获
            autoConstraintsCaptured = false;
        }
        else
        {
            // 计算跟随模式下期望的“相机参考位姿”（仅用于推导角度差）
            float currentY = transform.position.y;
            float targetY = target.transform.position.y;
            Vector3 flatDirection = transform.position - target.transform.position;
            flatDirection.y = 0f;
            if (flatDirection.sqrMagnitude < 0.0001f)
            {
                flatDirection = Vector3.back;
            }

            // 首次进入自动模式时，捕获自由模式的水平半径与 Y
            if (!autoConstraintsCaptured)
            {
                float flatRadius = new Vector2(flatDirection.x, flatDirection.z).magnitude;
                preservedFlatRadius = Mathf.Max(0.0001f, flatRadius);
                preservedRelativeY = currentY - targetY; // 相对目标的高度
                autoConstraintsCaptured = true;
            }

            // 使用“当前水平朝向的单位向量”与“保留的半径/Y”构造期望位置
            Vector3 desiredPos = target.transform.position + flatDirection.normalized * preservedFlatRadius;
            desiredPos.y = targetY + preservedRelativeY;

            autoFollowPivot.position = desiredPos;
            autoFollowPivot.LookAt(target.transform.position);

            // 由期望位置反解其 yaw/pitch
            Vector3 targetPosForAuto = target.transform.position;
            Vector3 offsetAuto = autoFollowPivot.position - targetPosForAuto;
            Vector3 dirAuto = offsetAuto.normalized;

            float desiredYaw = Mathf.Atan2(-dirAuto.x, -dirAuto.z) * Mathf.Rad2Deg;
            float horizontalLenAuto = Mathf.Sqrt(dirAuto.x * dirAuto.x + dirAuto.z * dirAuto.z);
            float desiredPitch = Mathf.Atan2(dirAuto.y, horizontalLenAuto) * Mathf.Rad2Deg;
            desiredPitch = Mathf.Clamp(desiredPitch, minPitchDeg, maxPitchDeg);

            // 转换为“输入增量”，走同一套轨道逻辑
            consumedYawDelta = ShortestDeltaAngle(yawDeg, desiredYaw);
            consumedPitchDelta = ShortestDeltaAngle(pitchDeg, desiredPitch);
            usedAutoInput = true;
        }

        // 累计角度（度），应用灵敏度
        yawDeg += consumedYawDelta * orbitSensitivityYaw;
        pitchDeg += consumedPitchDelta * orbitSensitivityPitch;

        // 清空输入队列
        hasQueuedInput = false;
        queuedYawDelta = 0f;
        queuedPitchDelta = 0f;

        // 夹角限制
        pitchDeg = Mathf.Clamp(pitchDeg, minPitchDeg, maxPitchDeg);

        // 计算新的相机位置：围绕目标以固定距离的轨道
        {
            Vector3 targetPos = target.transform.position;
            float usedDistance = Mathf.Max(0.0001f, distance);
            if (usedAutoInput)
            {
                // 调整半径以保持“水平半径”和“自由模式 Y”一致
                float cosPitch = Mathf.Cos(pitchDeg * Mathf.Deg2Rad);
                if (cosPitch < 0f) cosPitch = -cosPitch; // 仅需幅值
                cosPitch = Mathf.Max(0.0001f, cosPitch);
                usedDistance = preservedFlatRadius / cosPitch;
            }

            Vector3 offset = Quaternion.Euler(pitchDeg, yawDeg, 0f) * Vector3.back * usedDistance;
            transform.position = targetPos + offset;
            transform.LookAt(targetPos);
        }
    }

    // 初始化轨道角度：基于当前相机与目标的相对向量，避免首次切换跳变
    private void EnsureOrbitInitialized()
    {
        if (orbitInitialized)
        {
            return;
        }

        Vector3 targetPos = target.transform.position;
        Vector3 offset = transform.position - targetPos;
        if (offset.sqrMagnitude < 0.0001f)
        {
            // 若几乎重合，给出一个稳定偏移
            offset = Vector3.back * Mathf.Max(0.0001f, distance);
        }

        // 由偏移反解 yaw/pitch（度）
        Vector3 dir = offset.normalized;
        // yaw: 绕 Y 轴角度（x,z 平面）。注意轨道使用 Vector3.back，需用反向向量求解
        yawDeg = Mathf.Atan2(-dir.x, -dir.z) * Mathf.Rad2Deg;
        // pitch: 俯仰角（上为正）
        float horizontalLen = Mathf.Sqrt(dir.x * dir.x + dir.z * dir.z);
        pitchDeg = Mathf.Atan2(dir.y, horizontalLen) * Mathf.Rad2Deg;
        pitchDeg = Mathf.Clamp(pitchDeg, minPitchDeg, maxPitchDeg);

        orbitInitialized = true;
    }

    // 角度差（度）：返回从 from 到 to 的最小有向差值（范围约 [-180, 180]）
    private static float ShortestDeltaAngle(float fromDeg, float toDeg)
    {
        float delta = Mathf.DeltaAngle(fromDeg, toDeg);
        return delta;
    }
}
