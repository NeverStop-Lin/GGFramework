using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Framework.Core;

/// <summary>
/// 虚拟摇杆组件
/// 支持触摸、鼠标以及键盘输入 (WASD/方向键)
/// 支持订阅属性变化事件
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
    /// 是否正在控制中（包括手指按下但未操作、键盘输入等）
    /// 此状态会在输入归0后延迟一帧才变为false
    /// </summary>
    public bool IsControlling { get; private set; }

    /// <summary>
    /// Input 变化事件，参数为新值
    /// </summary>
    public Actions<Vector2> OnInputChanged { get; } = new Actions<Vector2>();

    /// <summary>
    /// IsControlling 状态变化事件，参数为新值
    /// </summary>
    public Actions<bool> OnIsControllingChanged { get; } = new Actions<bool>();

    /// <summary>
    /// 摇杆手柄的初始锚点位置（相对于背景的 anchoredPosition）
    /// 用于在松开时恢复手柄到中心位置
    /// </summary>
    private Vector2 _startHandlePos;
    
    /// <summary>
    /// 摇杆背景的初始本地位置（localPosition）
    /// 用于 Follow 模式下松开时背景归位
    /// </summary>
    private Vector2 _startBackgroundPos;
    
    /// <summary>
    /// 标记是否正在进行触摸/鼠标拖拽
    /// 与键盘输入互斥，拖拽时禁用键盘输入
    /// </summary>
    private bool _isDragging = false;
    
    /// <summary>
    /// 上一帧是否有输入（包括拖拽或键盘输入）
    /// 用于实现 IsControlling 状态的延迟一帧结束效果
    /// </summary>
    private bool _hadInputLastFrame = false;

    /// <summary>
    /// Follow 模式下的逻辑中心点（localPosition 坐标系）
    /// 触摸点击位置作为摇杆中心，用于计算输入值
    /// 独立于 UI 背景的缓动动画，确保输入计算精确
    /// </summary>
    private Vector2 _logicalCenter;
    
    /// <summary>
    /// Follow 模式下背景的目标位置（localPosition 坐标系）
    /// 配合 SmoothDamp 实现平滑缓动效果
    /// </summary>
    private Vector2 _targetBackgroundPos;
    
    /// <summary>
    /// 标记背景是否正在执行缓动动画
    /// true 时每帧更新背景位置，到达目标后自动设为 false
    /// </summary>
    private bool _isBackgroundMoving = false;
    
    /// <summary>
    /// 背景缓动的当前速度（Vector2.SmoothDamp 的 ref 参数）
    /// 用于实现先快后慢的平滑过渡效果
    /// </summary>
    private Vector2 _backgroundVelocity;

    void Start()
    {
        // 如果未指定触摸区域，使用脚本挂载的对象作为触摸区域
        if (joystickArea == null)
            joystickArea = GetComponent<RectTransform>();

        // 记录初始位置，用于归位
        _startHandlePos = joystickHandle.anchoredPosition;
        _startBackgroundPos = joystickBackground.localPosition;
    }

    /// <summary>
    /// Update 方法：处理键盘输入、背景缓动和控制状态更新
    /// </summary>
    void Update()
    {
        // ========== 键盘输入处理 ==========
        // 仅在未拖拽时处理键盘输入，避免冲突
        if (enableKeyboardInput && !_isDragging)
        {
            // 获取键盘输入（WASD 和方向键）
            Vector2 keyboardInput = new Vector2(UnityEngine.Input.GetAxisRaw("Horizontal"), UnityEngine.Input.GetAxisRaw("Vertical"));

            // 如果有键盘输入
            if (keyboardInput.magnitude > 0)
            {
                // 将键盘输入标准化（限制在单位圆内），并触发事件
                SetInput(keyboardInput.normalized);

                // 更新摇杆手柄的 UI 位置，提供视觉反馈
                joystickHandle.anchoredPosition = Input * (joystickBackground.sizeDelta.x / 2);
            }
            // 如果没有键盘输入，但上一帧有（即按键刚松开）
            else if (Input.magnitude > 0)
            {
                // 重置输入和 UI
                SetInput(Vector2.zero);
                joystickHandle.anchoredPosition = _startHandlePos;
            }
        }

        // ========== Follow 模式下的背景缓动 ==========
        // 独立执行，不受键盘输入影响
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

            // 到达目标位置后停止缓动（距离阈值 0.5 像素）
            if (Vector2.Distance(joystickBackground.localPosition, _targetBackgroundPos) < 0.5f)
            {
                joystickBackground.localPosition = _targetBackgroundPos;
                _backgroundVelocity = Vector2.zero;
                _isBackgroundMoving = false;
            }
        }

        // ========== 更新控制状态（延迟一帧变化） ==========
        // 判断当前帧是否有任何输入
        bool hasInputThisFrame = _isDragging || Input.magnitude > 0;
        
        // IsControlling 在当前帧或上一帧有输入时为 true（延迟一帧结束）
        // 这样可以避免抖动，让外部有机会处理最后一帧的输入
        SetIsControlling(hasInputThisFrame || _hadInputLastFrame);
        
        // 记录当前帧状态，供下一帧判断
        _hadInputLastFrame = hasInputThisFrame;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // 标记开始拖拽，禁用键盘输入
        _isDragging = true;

        // Follow 模式：摇杆跟随点击位置
        if (joystickMode == JoystickMode.Follow)
        {
            Vector2 localPoint;
            // 将屏幕坐标转换为 joystickArea 的本地坐标
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                joystickArea,
                eventData.position,
                eventData.pressEventCamera,
                out localPoint);

            // 立即记录逻辑中心，用于后续输入计算
            // 逻辑中心固定不变，UI 背景可以平滑缓动
            _logicalCenter = localPoint;
            
            // 设置背景目标位置并启动缓动
            _targetBackgroundPos = localPoint;
            _backgroundVelocity = Vector2.zero; // 重置速度，确保平滑过渡
            _isBackgroundMoving = true;
        }
        
        // 立即调用 OnDrag，开始计算输入
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 只有在拖拽状态下才处理
        if (!_isDragging) return;

        Vector2 pos;

        // ========== Follow 模式 ==========
        if (joystickMode == JoystickMode.Follow)
        {
            // Follow 模式：基于逻辑中心计算，不受 UI 缓动影响
            Vector2 touchLocalPos;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    joystickArea,
                    eventData.position,
                    eventData.pressEventCamera,
                    out touchLocalPos))
            {
                // 计算触摸点相对于逻辑中心的偏移
                pos = touchLocalPos - _logicalCenter;

                // 将像素偏移转换为归一化输入值（-1 到 1）
                float inputX = pos.x / (joystickBackground.sizeDelta.x / 2);
                float inputY = pos.y / (joystickBackground.sizeDelta.y / 2);
                Vector2 newInput = new Vector2(inputX, inputY);
                
                // 限制在单位圆内（最大值为 1）
                if (newInput.magnitude > 1.0f)
                {
                    newInput = newInput.normalized;
                }
                
                // 更新输入值并触发事件
                SetInput(newInput);
                
                // 更新手柄 UI 位置
                joystickHandle.anchoredPosition = new Vector2(
                    Input.x * (joystickBackground.sizeDelta.x / 2),
                    Input.y * (joystickBackground.sizeDelta.y / 2)
                );
            }
        }
        // ========== Fixed 模式 ==========
        else
        {
            // Fixed 模式：摇杆固定在初始位置
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    joystickBackground,
                    eventData.position,
                    eventData.pressEventCamera,
                    out pos))
            {
                // 将像素偏移转换为归一化输入值（-1 到 1）
                float inputX = pos.x / (joystickBackground.sizeDelta.x / 2);
                float inputY = pos.y / (joystickBackground.sizeDelta.y / 2);
                Vector2 newInput = new Vector2(inputX, inputY);
                
                // 限制在单位圆内（最大值为 1）
                if (newInput.magnitude > 1.0f)
                {
                    newInput = newInput.normalized;
                }
                
                // 更新输入值并触发事件
                SetInput(newInput);
                
                // 更新手柄 UI 位置
                joystickHandle.anchoredPosition = new Vector2(
                    Input.x * (joystickBackground.sizeDelta.x / 2),
                    Input.y * (joystickBackground.sizeDelta.y / 2)
                );
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // 重置输入值为零
        SetInput(Vector2.zero);
        
        // 结束拖拽，允许键盘输入
        _isDragging = false;
        
        // 手柄归位到中心
        joystickHandle.anchoredPosition = _startHandlePos;

        // Follow 模式下，背景也要归位（使用缓动）
        if (joystickMode == JoystickMode.Follow)
        {
            _targetBackgroundPos = _startBackgroundPos;
            _backgroundVelocity = Vector2.zero; // 重置速度，确保平滑过渡
            _isBackgroundMoving = true;
        }
    }

    /// <summary>
    /// 设置 Input 并触发变化事件
    /// 只有当值真正改变时才会触发事件，避免无效通知
    /// </summary>
    private void SetInput(Vector2 newValue)
    {
        if (Input != newValue)
        {
            Input = newValue;
            OnInputChanged.Invoke(newValue);
        }
    }

    /// <summary>
    /// 设置 IsControlling 并触发变化事件
    /// 只有当状态真正改变时才会触发事件，避免无效通知
    /// </summary>
    private void SetIsControlling(bool newValue)
    {
        if (IsControlling != newValue)
        {
            IsControlling = newValue;
            OnIsControllingChanged.Invoke(newValue);
        }
    }
}