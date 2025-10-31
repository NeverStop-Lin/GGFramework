using UnityEngine;
using Framework.Core;

/// <summary>
/// UI 输入组件使用示例
/// 展示如何订阅 TouchDragInput 和 VirtualJoystick 的事件
/// </summary>
public class InputWidgetExample : MonoBehaviour
{
    [Header("输入组件引用")]
    [SerializeField] private TouchDragInput touchDragInput;
    [SerializeField] private VirtualJoystick virtualJoystick;

    [Header("控制参数")]
    [SerializeField] private float rotationSpeed = 0.1f;
    [SerializeField] private float moveSpeed = 5f;

    private Vector3 _moveDirection;
    private bool _isRotating;

    void Start()
    {
        // 订阅触摸拖拽事件
        if (touchDragInput != null)
        {
            touchDragInput.OnDragDeltaChanged.Add(OnDragDeltaChanged, this);
            touchDragInput.OnIsDraggingChanged.Add(OnDraggingStateChanged, this);
        }

        // 订阅虚拟摇杆事件
        if (virtualJoystick != null)
        {
            virtualJoystick.OnInputChanged.Add(OnJoystickInputChanged, this);
            virtualJoystick.OnIsControllingChanged.Add(OnControllingStateChanged, this);
        }
    }

    void OnDestroy()
    {
        // 取消订阅，防止内存泄漏
        if (touchDragInput != null)
        {
            touchDragInput.OnDragDeltaChanged.RemoveTarget(this);
            touchDragInput.OnIsDraggingChanged.RemoveTarget(this);
        }

        if (virtualJoystick != null)
        {
            virtualJoystick.OnInputChanged.RemoveTarget(this);
            virtualJoystick.OnIsControllingChanged.RemoveTarget(this);
        }
    }

    void Update()
    {
        // 应用移动（在 Update 中处理，确保平滑）
        if (_moveDirection.magnitude > 0.01f)
        {
            transform.Translate(_moveDirection * moveSpeed * Time.deltaTime, Space.World);
        }
    }

    #region TouchDragInput 回调

    /// <summary>
    /// 拖拽增量变化回调
    /// </summary>
    private void OnDragDeltaChanged(Vector2 delta)
    {
        if (delta.magnitude > 0.01f)
        {
            // 根据拖拽增量旋转物体
            transform.Rotate(Vector3.up, -delta.x * rotationSpeed, Space.World);
            transform.Rotate(transform.right, delta.y * rotationSpeed, Space.World);
            
            Debug.Log($"[TouchDrag] Delta: {delta}");
        }
    }

    /// <summary>
    /// 拖拽状态变化回调
    /// </summary>
    private void OnDraggingStateChanged(bool isDragging)
    {
        _isRotating = isDragging;
        Debug.Log($"[TouchDrag] Is Dragging: {isDragging}");
    }

    #endregion

    #region VirtualJoystick 回调

    /// <summary>
    /// 摇杆输入变化回调
    /// </summary>
    private void OnJoystickInputChanged(Vector2 input)
    {
        // 将 2D 输入转换为 3D 移动方向
        _moveDirection = new Vector3(input.x, 0, input.y);
        
        if (input.magnitude > 0.01f)
        {
            Debug.Log($"[Joystick] Input: {input}");
        }
    }

    /// <summary>
    /// 控制状态变化回调
    /// </summary>
    private void OnControllingStateChanged(bool isControlling)
    {
        Debug.Log($"[Joystick] Is Controlling: {isControlling}");
        
        if (!isControlling)
        {
            // 停止控制时的清理逻辑
            _moveDirection = Vector3.zero;
        }
    }

    #endregion
}

