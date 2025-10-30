using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 虚拟摇杆组件
/// 支持触摸、鼠标以及键盘输入 (WASD/方向键)
/// </summary>
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    // --- 枚举定义 ---
    public enum JoystickMode { Fixed, Follow }

    [Header("模式设置")]
    [Tooltip("选择摇杆的工作模式：Fixed=固定位置, Follow=点击位置为中心")]
    [SerializeField]
    private JoystickMode joystickMode = JoystickMode.Follow;

    // --- 新增的键盘设置 ---
    [Header("键盘设置")]
    [Tooltip("是否启用键盘输入 (WASD 和 方向键)")]
    [SerializeField]
    private bool enableKeyboardInput = true;

    [Header("缓动设置")]
    [Tooltip("Follow 模式下背景移动的平滑时间（值越小移动越快，建议 0.05-0.2）")]
    [SerializeField]
    private float backgroundSmoothTime = 0.05f;

    [Header("UI 引用")]
    [Tooltip("摇杆的可触摸区域（脚本挂载对象）")]
    [SerializeField]
    private RectTransform joystickArea;
    [Tooltip("摇杆背景图，将作为摇杆的中心")]
    [SerializeField]
    private RectTransform joystickBackground;
    [Tooltip("摇杆手柄")]
    [SerializeField]
    private RectTransform joystickHandle;

    /// <summary>
    /// 公开的摇杆输入值，范围为(-1, -1)到(1, 1)
    /// </summary>
    public Vector2 Input { get; private set; }
    /// <summary>
    /// 是否拖拽
    /// </summary>
    public bool IsDragging => _isDragging;

    private Vector2 _startHandlePos;
    private Vector2 _startBackgroundPos;
    private bool _isDragging = false; // 标记是否正在进行触摸/鼠标拖拽
    
    private Vector2 _logicalCenter; // 逻辑中心（用于输入计算，localPosition坐标系）
    private Vector2 _targetBackgroundPos; // 背景目标位置（用于缓动，localPosition坐标系）
    private bool _isBackgroundMoving = false; // 背景是否正在缓动
    private Vector2 _backgroundVelocity; // 背景缓动速度（用于 SmoothDamp）

    void Start()
    {
        if (joystickArea == null)
            joystickArea = GetComponent<RectTransform>();

        _startHandlePos = joystickHandle.anchoredPosition;
        _startBackgroundPos = joystickBackground.localPosition;
    }

    /// <summary>
    /// 新增的 Update 方法，用于处理键盘输入和背景缓动
    /// </summary>
    void Update()
    {
        // 键盘输入处理
        if (enableKeyboardInput && !_isDragging)
        {
            // 获取键盘输入
            Vector2 keyboardInput = new Vector2(UnityEngine.Input.GetAxisRaw("Horizontal"), UnityEngine.Input.GetAxisRaw("Vertical"));

            // 如果有键盘输入
            if (keyboardInput.magnitude > 0)
            {
                // 将键盘输入标准化，并赋值给公共 Input 属性
                Input = keyboardInput.normalized;

                // 更新摇杆手柄的UI位置，提供视觉反馈
                joystickHandle.anchoredPosition = Input * (joystickBackground.sizeDelta.x / 2);
            }
            // 如果没有键盘输入，但上一帧有（即按键刚松开）
            else if (Input.magnitude > 0)
            {
                // 重置输入和UI
                Input = Vector2.zero;
                joystickHandle.anchoredPosition = _startHandlePos;
            }
        }

        // Follow 模式下的背景缓动（独立执行，不受键盘输入影响）
        if (joystickMode == JoystickMode.Follow && _isBackgroundMoving)
        {
            Vector2 currentPos = joystickBackground.localPosition;
            // 使用 SmoothDamp 实现先快后慢的平滑缓动效果
            joystickBackground.localPosition = Vector2.SmoothDamp(
                currentPos, 
                _targetBackgroundPos, 
                ref _backgroundVelocity,
                backgroundSmoothTime
            );
            
            // 到达目标位置后停止缓动
            if (Vector2.Distance(joystickBackground.localPosition, _targetBackgroundPos) < 0.5f)
            {
                joystickBackground.localPosition = _targetBackgroundPos;
                _backgroundVelocity = Vector2.zero;
                _isBackgroundMoving = false;
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _isDragging = true; // 开始拖拽

        if (joystickMode == JoystickMode.Follow)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                joystickArea,
                eventData.position,
                eventData.pressEventCamera,
                out localPoint);
            
            // 立即记录逻辑中心，用于后续输入计算
            _logicalCenter = localPoint;
            // 设置背景目标位置并启动缓动
            _targetBackgroundPos = localPoint;
            _backgroundVelocity = Vector2.zero; // 重置速度，确保平滑过渡
            _isBackgroundMoving = true;
        }
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 只有在拖拽状态下才处理
        if (!_isDragging) return;

        Vector2 pos;
        
        if (joystickMode == JoystickMode.Follow)
        {
            // Follow 模式：基于逻辑中心计算，不受UI缓动影响
            Vector2 touchLocalPos;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    joystickArea,
                    eventData.position,
                    eventData.pressEventCamera,
                    out touchLocalPos))
            {
                // 计算触摸点相对于逻辑中心的偏移
                pos = touchLocalPos - _logicalCenter;
                
                float inputX = pos.x / (joystickBackground.sizeDelta.x / 2);
                float inputY = pos.y / (joystickBackground.sizeDelta.y / 2);
                Input = new Vector2(inputX, inputY);
                if (Input.magnitude > 1.0f)
                {
                    Input = Input.normalized;
                }
                joystickHandle.anchoredPosition = new Vector2(
                    Input.x * (joystickBackground.sizeDelta.x / 2),
                    Input.y * (joystickBackground.sizeDelta.y / 2)
                );
            }
        }
        else
        {
            // Fixed 模式：保持原有逻辑
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    joystickBackground,
                    eventData.position,
                    eventData.pressEventCamera,
                    out pos))
            {
                float inputX = pos.x / (joystickBackground.sizeDelta.x / 2);
                float inputY = pos.y / (joystickBackground.sizeDelta.y / 2);
                Input = new Vector2(inputX, inputY);
                if (Input.magnitude > 1.0f)
                {
                    Input = Input.normalized;
                }
                joystickHandle.anchoredPosition = new Vector2(
                    Input.x * (joystickBackground.sizeDelta.x / 2),
                    Input.y * (joystickBackground.sizeDelta.y / 2)
                );
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isDragging = false; // 结束拖拽

        Input = Vector2.zero;
        joystickHandle.anchoredPosition = _startHandlePos;
        
        // Follow 模式下，背景也要归位（使用缓动）
        if (joystickMode == JoystickMode.Follow)
        {
            _targetBackgroundPos = _startBackgroundPos;
            _backgroundVelocity = Vector2.zero; // 重置速度，确保平滑过渡
            _isBackgroundMoving = true;
        }
    }
}