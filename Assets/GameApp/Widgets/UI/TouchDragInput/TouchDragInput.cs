using UnityEngine;
using UnityEngine.EventSystems;
using Framework.Core;

/// <summary>
/// 触摸拖拽输入组件
/// 提供屏幕空间的拖拽增量和按压状态，不包含任何业务逻辑
/// 支持订阅属性变化事件
/// </summary>
public class TouchDragInput : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    /// <summary>
    /// 本帧的拖拽增量（屏幕像素），仅在拖拽时有效，下一帧自动归零
    /// </summary>
    public Vector2 DragDelta { get; private set; }

    /// <summary>
    /// 是否正在按住屏幕拖拽
    /// </summary>
    public bool IsDragging { get; private set; }

    /// <summary>
    /// DragDelta 变化事件，参数为新值
    /// </summary>
    public Actions<Vector2> OnDragDeltaChanged { get; } = new Actions<Vector2>();

    /// <summary>
    /// IsDragging 状态变化事件，参数为新值
    /// </summary>
    public Actions<bool> OnIsDraggingChanged { get; } = new Actions<bool>();

    /// <summary>
    /// 上一次指针的屏幕位置，用于计算每帧的拖拽增量
    /// </summary>
    private Vector2 _lastPointerPosition;

    /// <summary>
    /// 当前帧累积的拖拽增量暂存区（防止低帧率时丢失输入）
    /// 在 Update 中转移到 DragDelta 后立即清空
    /// </summary>
    private Vector2 _currentFrameDelta;

    void Update()
    {
        // 每帧开始时，先将上一帧的增量重置为 0
        SetDragDelta(Vector2.zero);

        // 未拖拽时不处理增量
        if (!IsDragging)
        {
            return;
        }

        // 将暂存的增量赋值给公开属性（会触发 OnDragDeltaChanged 事件）
        SetDragDelta(_currentFrameDelta);

        // 消费后立即清空暂存区，防止重复使用
        _currentFrameDelta = Vector2.zero;
    }

    public void OnPointerDown(PointerEventData eventData)
    {

        // 设置拖拽状态为 true，触发事件
        SetIsDragging(true);

        // 记录初始指针位置
        _lastPointerPosition = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 非拖拽状态不处理
        if (!IsDragging)
        {
            return;
        }

        Vector2 currentPosition = eventData.position;

        // 计算本次事件的增量并累加到暂存区
        // 累加机制防止在低帧率时丢失 OnDrag 事件的输入
        _currentFrameDelta += currentPosition - _lastPointerPosition;

        // 更新位置，用于下次计算增量
        _lastPointerPosition = currentPosition;
    }

    public void OnPointerUp(PointerEventData eventData)
    {

        // 设置拖拽状态为 false，触发事件
        SetIsDragging(false);

        // 清空暂存区，防止松开后残留增量
        _currentFrameDelta = Vector2.zero;
    }

    /// <summary>
    /// 设置 DragDelta 并触发变化事件
    /// 只有当值真正改变时才会触发事件，避免无效通知
    /// </summary>
    private void SetDragDelta(Vector2 newValue)
    {
        if (DragDelta != newValue)
        {
            DragDelta = newValue;
            OnDragDeltaChanged.Invoke(newValue);
        }
    }

    /// <summary>
    /// 设置 IsDragging 并触发变化事件
    /// 只有当状态真正改变时才会触发事件，避免无效通知
    /// </summary>
    private void SetIsDragging(bool newValue)
    {
        if (IsDragging != newValue)
        {
            IsDragging = newValue;
            OnIsDraggingChanged.Invoke(newValue);
        }
    }
}
