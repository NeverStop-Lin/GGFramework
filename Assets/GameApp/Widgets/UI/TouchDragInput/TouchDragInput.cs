using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 触摸拖拽输入组件
/// 提供屏幕空间的拖拽增量和按压状态，不包含任何业务逻辑
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

    private Vector2 _lastPointerPosition;
    private Vector2 _currentFrameDelta;

    void Update()
    {
        // 每帧开始时重置增量
        DragDelta = Vector2.zero;

        if (!IsDragging)
        {
            return;
        }

        // 将暂存的增量赋值给公开属性
        DragDelta = _currentFrameDelta;
        // 消费后立即清空暂存区
        _currentFrameDelta = Vector2.zero;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("OnPointerDown");
        IsDragging = true;
        _lastPointerPosition = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsDragging)
        {
            return;
        }

        Vector2 currentPosition = eventData.position;
        // 累加增量，防止低帧率时丢失输入
        _currentFrameDelta += currentPosition - _lastPointerPosition;
        _lastPointerPosition = currentPosition;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("OnPointerUp");
        IsDragging = false;
        _currentFrameDelta = Vector2.zero;
    }
}
