# 角色控制器优化 - 实施计划

基于 `DesignDecisions.md` 中的所有决策，本文档提供详细的实施步骤。

---

## 实施概览

### 目标
优化 Character 文件夹中的角色控制器，采用基于时间的跳跃系统，简化功能并改进实现方式。

### 工作量估算
- 修改文件：3个
- 删除文件：1个
- 预计工时：2-3小时

---

## 实施步骤

### 第一步：修改 CharacterControllerConfig.cs

**文件路径**: `Assets/GameApp/Scripts/Character/Config/CharacterControllerConfig.cs`

#### 1.1 删除高级功能配置参数
移除以下所有字段和 Header：
- 飞扑相关：`DashSpeed`, `DashDuration`, `DashDecay`, `DashCooldown`, `AirDashConsumesJump`
- 蹲伏相关：`CrouchedHeight`, `StandingHeight`, `CapsuleRadius`, `CrouchedSpeedMultiplier`
- 梯子相关：`LadderDetectionRadius`, `LadderDetectionDistance`, `LadderClimbSpeed`, `LadderTag`, `LadderLayer`
- 爬墙相关：`ClimbableWallLayer`, `MaxWallClimbAngle`, `WallStickDistance`, `WallSlideSpeed`, `MaxWallGrabDuration`, `WallJumpUpSpeed`, `WallJumpBackSpeed`

#### 1.2 修改跳跃配置参数
**移除**:
- `JumpUpSpeed`
- `JumpForwardSpeed`
- `AllowJumpingWhenSliding`

**新增**:
```csharp
[Header("跳跃（新系统）")]
[Label("跳跃持续时间", "跳跃力施加的时间")]
public float JumpDuration = 0.4f;

[Label("基础跳跃速度", "配合曲线控制跳跃高度")]
public float JumpSpeed = 12f;

[Label("跳跃速度曲线", "X=时间进度0-1，Y=速度倍率0-1")]
public AnimationCurve JumpSpeedCurve;
```

#### 1.3 保留的参数
确保以下参数保持不变：
- 基础移动：`MaxStableMoveSpeed`, `StableMovementSharpness`, `OrientationSharpness`
- 空中移动：`MaxAirMoveSpeed`, `AirAcceleration`, `AirDrag`, `Gravity`
- 跳跃系统：`MaxJumpCount`, `CoyoteTime`, `JumpBufferTime`

---

### 第二步：修改 CharacterState.cs

**文件路径**: `Assets/GameApp/Scripts/Character/Core/CharacterState.cs`

#### 2.1 简化状态枚举
```csharp
namespace GameApp.Character
{
    /// <summary>
    /// 角色运动状态
    /// </summary>
    public enum CharacterState
    {
        Default,   // 地面状态
        Airborne,  // 空中状态
        Jumping,   // 跳跃中状态
    }
}
```

**移除的状态**:
- `Dash`
- `Crouch`
- `Ladder`
- `WallClimb`

---

### 第三步：重构 CharacterController.cs

**文件路径**: `Assets/GameApp/Scripts/Character/Core/CharacterController.cs`

这是最复杂的部分，分多个子步骤完成。

#### 3.1 定义 JumpState 结构体

在 `CharacterController` 类内部添加：

```csharp
private struct JumpState
{
    public bool IsActive;                // 跳跃是否激活
    public float ElapsedTime;            // 已经过的时间
    public int ConsumedCount;            // 已消耗的跳跃次数
    public float CoyoteTimeRemaining;    // 剩余土狼时间
    public float BufferTimeRemaining;    // 剩余跳跃缓冲时间
    
    public void Reset()
    {
        IsActive = false;
        ElapsedTime = 0f;
        ConsumedCount = 0;
        CoyoteTimeRemaining = 0f;
        BufferTimeRemaining = 0f;
    }
}
```

#### 3.2 更新私有字段

**移除**:
```csharp
// 删除所有飞扑、爬墙、梯子、蹲伏相关的私有字段
_dashOnCooldown, _dashTimeRemaining, _dashDirection, _dashCooldownTimer
_currentLadder
_lastWallNormal, _wallGrabTimer
_isCrouching
_probedColliders
```

**新增/修改**:
```csharp
// 相机引用
private Camera _mainCamera;

// 跳跃状态（替换原有的跳跃相关字段）
private JumpState _jumpState;

// 简化后的字段
private Vector3 _moveInputVector = Vector3.zero;
private Vector3 _desiredForward = Vector3.forward;
```

#### 3.3 修改 SetInput 方法

**新签名**:
```csharp
public void SetInput(ref CharacterInputData input)
{
    // 1. 处理移动输入（支持两种模式）
    if (input.UseCameraRotation)
    {
        // 模式1：原始输入 + 相机旋转转换
        Quaternion cameraRotation = input.CameraRotation != Quaternion.identity 
            ? input.CameraRotation 
            : (_mainCamera != null ? _mainCamera.transform.rotation : Quaternion.identity);
        
        Vector3 cameraPlanarDirection = 
            Vector3.ProjectOnPlane(cameraRotation * Vector3.forward, Motor.CharacterUp).normalized;
        if (cameraPlanarDirection.sqrMagnitude == 0f)
        {
            cameraPlanarDirection = 
                Vector3.ProjectOnPlane(cameraRotation * Vector3.up, Motor.CharacterUp).normalized;
        }
        
        Quaternion cameraPlanarRotation = Quaternion.LookRotation(cameraPlanarDirection, Motor.CharacterUp);
        Vector3 rawInput = new Vector3(input.MoveInput.x, 0f, input.MoveInput.y);
        _moveInputVector = cameraPlanarRotation * Vector3.ClampMagnitude(rawInput, 1f);
    }
    else
    {
        // 模式2：已转换的世界空间输入
        _moveInputVector = new Vector3(input.MoveInput.x, 0f, input.MoveInput.y);
        if (_moveInputVector.sqrMagnitude > 1f)
        {
            _moveInputVector.Normalize();
        }
    }
    
    // 2. 朝向：取移动方向
    if (_moveInputVector.sqrMagnitude > 0.0001f)
    {
        _desiredForward = _moveInputVector;
    }
    
    // 3. 跳跃缓冲
    if (input.JumpDown)
    {
        _jumpState.BufferTimeRemaining = Config.JumpBufferTime;
    }
}
```

#### 3.4 添加 SetCamera 方法

```csharp
/// <summary>
/// 设置相机引用（用于获取相机旋转）
/// </summary>
public void SetCamera(Camera camera)
{
    _mainCamera = camera;
}
```

#### 3.5 重构 UpdateVelocity 方法

这是核心方法，需要完全重写。

**核心结构**:
```csharp
public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
{
    bool isStableOnGround = Motor.GroundingStatus.IsStableOnGround;
    
    // 1. 更新土狼时间和跳跃缓冲
    UpdateJumpTimers(isStableOnGround, deltaTime);
    
    // 2. 状态维护（着地/离地转换）
    UpdateCharacterState(isStableOnGround);
    
    // 3. 跳跃处理
    HandleJumping(ref currentVelocity, isStableOnGround, deltaTime);
    
    // 4. 移动处理（地面/空中）
    HandleMovement(ref currentVelocity, isStableOnGround, deltaTime);
    
    // 5. 重力施加
    currentVelocity += Config.Gravity * deltaTime;
    
    // 6. 空中阻力
    if (!isStableOnGround)
    {
        currentVelocity *= (1f / (1f + (Config.AirDrag * deltaTime)));
    }
    
    // 7. 检测头顶碰撞（速度截断）
    DetectHeadCollision(currentVelocity, deltaTime);
}
```

**辅助方法**:

```csharp
private void UpdateJumpTimers(bool isGrounded, float deltaTime)
{
    _jumpState.BufferTimeRemaining -= deltaTime;
    
    if (isGrounded)
    {
        _jumpState.CoyoteTimeRemaining = Config.CoyoteTime;
        _jumpState.ConsumedCount = 0;
    }
    else
    {
        _jumpState.CoyoteTimeRemaining -= deltaTime;
    }
}

private void UpdateCharacterState(bool isGrounded)
{
    if (isGrounded && (_currentState == CharacterState.Airborne || _currentState == CharacterState.Jumping))
    {
        TransitionToState(CharacterState.Default);
    }
    else if (!isGrounded && _currentState == CharacterState.Default && !_jumpState.IsActive)
    {
        TransitionToState(CharacterState.Airborne);
    }
}

private void HandleJumping(ref Vector3 currentVelocity, bool isGrounded, float deltaTime)
{
    // 检查是否可以跳跃
    bool canJump = (isGrounded || _jumpState.CoyoteTimeRemaining > 0f) || 
                   (_jumpState.ConsumedCount < Config.MaxJumpCount);
    bool bufferValid = _jumpState.BufferTimeRemaining > 0f;
    
    if (canJump && bufferValid && !_jumpState.IsActive)
    {
        // 开始新跳跃
        _jumpState.IsActive = true;
        _jumpState.ElapsedTime = 0f;
        _jumpState.BufferTimeRemaining = 0f;
        
        if (isGrounded || _jumpState.CoyoteTimeRemaining > 0f)
        {
            _jumpState.ConsumedCount = 1;
        }
        else
        {
            _jumpState.ConsumedCount++;
        }
        
        TransitionToState(CharacterState.Jumping);
        Motor.ForceUnground();
        
        // 保留部分向上速度
        Vector3 verticalVelocity = Vector3.Project(currentVelocity, Motor.CharacterUp);
        if (Vector3.Dot(verticalVelocity, Motor.CharacterUp) > 0f)
        {
            // 保留向上分量
        }
        else
        {
            // 清除向下分量
            currentVelocity -= verticalVelocity;
        }
    }
    
    // 施加跳跃力
    if (_jumpState.IsActive)
    {
        _jumpState.ElapsedTime += deltaTime;
        float progress = _jumpState.ElapsedTime / Config.JumpDuration;
        
        if (progress < 1.0f)
        {
            AnimationCurve curve = Config.JumpSpeedCurve ?? GetDefaultJumpCurve();
            float speedMultiplier = curve.Evaluate(progress);
            currentVelocity += Motor.CharacterUp * (Config.JumpSpeed * speedMultiplier * deltaTime);
        }
        else
        {
            // 跳跃结束
            _jumpState.IsActive = false;
            if (!isGrounded)
            {
                TransitionToState(CharacterState.Airborne);
            }
        }
    }
}

private void HandleMovement(ref Vector3 currentVelocity, bool isGrounded, float deltaTime)
{
    if (isGrounded)
    {
        // 地面移动（融合 DivCharacterController 的切线计算）
        float currentSpeedMagnitude = currentVelocity.magnitude;
        Vector3 effectiveGroundNormal = Motor.GroundingStatus.GroundNormal;
        currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, effectiveGroundNormal) * currentSpeedMagnitude;
        
        // 目标速度
        Vector3 inputRight = Vector3.Cross(_moveInputVector, Motor.CharacterUp);
        Vector3 reorientedInput = Vector3.Cross(effectiveGroundNormal, inputRight).normalized * _moveInputVector.magnitude;
        Vector3 targetVelocity = reorientedInput * Config.MaxStableMoveSpeed;
        
        // Lerp 平滑
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, 
                                       1f - Mathf.Exp(-Config.StableMovementSharpness * deltaTime));
    }
    else
    {
        // 空中移动（简化 + 防爬坡）
        if (_moveInputVector.sqrMagnitude > 0f)
        {
            Vector3 targetVelocity = _moveInputVector * Config.MaxAirMoveSpeed;
            
            // 防爬坡
            if (Motor.GroundingStatus.FoundAnyGround)
            {
                Vector3 perpenticularObstructionNormal = 
                    Vector3.Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal), 
                                 Motor.CharacterUp).normalized;
                targetVelocity = Vector3.ProjectOnPlane(targetVelocity, perpenticularObstructionNormal);
            }
            
            // 空中加速
            currentVelocity += (targetVelocity - currentVelocity) * Config.AirAcceleration * deltaTime;
        }
    }
}

private void DetectHeadCollision(Vector3 velocityBeforeJump, float deltaTime)
{
    // 速度截断检测（在 Motor.Move 后调用）
    // 注：需要在调用处传入施加跳跃力之前的速度
}

private AnimationCurve GetDefaultJumpCurve()
{
    // 创建默认平台跳曲线
    AnimationCurve curve = new AnimationCurve();
    curve.AddKey(new Keyframe(0f, 1.0f));
    curve.AddKey(new Keyframe(0.8f, 0.8f));
    curve.AddKey(new Keyframe(1.0f, 0f));
    
    // 设置为平滑插值
    for (int i = 0; i < curve.keys.Length; i++)
    {
        curve.SmoothTangents(i, 0f);
    }
    
    return curve;
}
```

#### 3.6 重构 UpdateRotation 方法

简化为只处理默认旋转：

```csharp
public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
{
    Vector3 currentUp = currentRotation * Vector3.up;
    
    if (_desiredForward.sqrMagnitude > 0.0001f)
    {
        // Slerp 球形插值
        Vector3 targetForward = Vector3.Slerp(Motor.CharacterForward, _desiredForward,
            1f - Mathf.Exp(-Config.OrientationSharpness * deltaTime)).normalized;
        currentRotation = Quaternion.LookRotation(targetForward, currentUp);
    }
    
    // 对齐世界上方向
    Vector3 smoothedGravityUp = Vector3.Slerp(currentUp, Vector3.up, 
                                              1f - Mathf.Exp(-Config.OrientationSharpness * deltaTime));
    currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityUp) * currentRotation;
}
```

#### 3.7 修改 OnMovementHit 回调

添加头顶碰撞检测：

```csharp
public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
    ref HitStabilityReport hitStabilityReport)
{
    // 检测头顶碰撞
    if (Vector3.Dot(hitNormal, Motor.CharacterUp) < -0.7f)
    {
        if (_jumpState.IsActive)
        {
            _jumpState.IsActive = false;
            if (!Motor.GroundingStatus.IsStableOnGround)
            {
                TransitionToState(CharacterState.Airborne);
            }
        }
    }
}
```

#### 3.8 删除高级功能相关方法

移除以下所有方法：
- `ToggleCrouch()`
- `StartDash()`
- `DetectLadder()`
- `IsClimbableWallNormal()`
- `IsStillFacingClimbableWall()`
- Dash/Ladder/WallClimb 状态的所有处理逻辑

#### 3.9 简化 TransitionToState 方法

```csharp
private void TransitionToState(CharacterState newState)
{
    _currentState = newState;
}
```

#### 3.10 更新 Awake 方法

```csharp
private void Awake()
{
    if (Motor == null)
    {
        throw new InvalidOperationException("KinematicCharacterMotor is required on the same GameObject");
    }
    if (Config == null)
    {
        throw new InvalidOperationException("CharacterControllerConfig is missing");
    }

    Motor.CharacterController = this;
    TransitionToState(CharacterState.Default);
    _jumpState.Reset();
}
```

#### 3.11 代码组织（Region 划分）

重新组织代码，使用 `#region` 分组：

```csharp
#region Configuration
// 序列化字段（Motor, MeshRoot, Config）
#endregion

#region State
// 私有状态字段（_currentState, _jumpState, _moveInputVector, _desiredForward, _mainCamera）
// JumpState 结构体定义
#endregion

#region Motor Interface
// ICharacterController 接口实现
// BeforeCharacterUpdate, UpdateRotation, UpdateVelocity, AfterCharacterUpdate
// PostGroundingUpdate, IsColliderValidForCollisions
// OnGroundHit, OnMovementHit, AddVelocity, ProcessHitStabilityReport, OnDiscreteCollisionDetected
#endregion

#region Internal Logic
// SetInput, SetCamera
// UpdateJumpTimers, UpdateCharacterState, HandleJumping, HandleMovement
// DetectHeadCollision, GetDefaultJumpCurve, TransitionToState
#endregion
```

---

### 第四步：删除 CharacterInput.cs

**文件路径**: `Assets/GameApp/Scripts/Character/Core/CharacterInput.cs`

#### 4.1 备份旧代码
在删除前，建议在决策文档中记录 `CharacterInput` 的原有逻辑，以便后续如果需要可以参考。

#### 4.2 删除文件
直接删除 `CharacterInput.cs` 及其 `.meta` 文件。

#### 4.3 更新 CharacterInputData
确保 `CharacterInputData` 结构体保留在合适的位置（建议放在 `CharacterController.cs` 文件的 `namespace` 级别）：

```csharp
namespace GameApp.Character
{
    /// <summary>
    /// 输入数据结构（由外部输入系统组装）
    /// </summary>
    public struct CharacterInputData
    {
        public Vector2 MoveInput;         // 移动输入（-1到1）或世界空间方向
        public bool JumpDown;             // 跳跃按钮按下
        public Quaternion CameraRotation; // 相机旋转（可选）
        public bool UseCameraRotation;    // 是否使用相机旋转进行坐标转换
    }
}
```

---

### 第五步：测试与验证

#### 5.1 编译检查
1. 保存所有文件
2. 让 Unity 重新编译
3. 修复所有编译错误

#### 5.2 配置检查
1. 在 Project 窗口找到现有的 `CharacterControllerConfig` 资源
2. 检查新增参数是否显示
3. 设置推荐默认值（参考决策文档 7.2 节）
4. 为 `JumpSpeedCurve` 设置默认曲线：
   - 添加关键帧：(0, 1.0), (0.8, 0.8), (1.0, 0)
   - 确保插值模式为 Smooth

#### 5.3 场景测试
1. 打开测试场景
2. 确保角色 GameObject 上：
   - 有 `CharacterController` 组件
   - 有 `KinematicCharacterMotor` 组件
   - `Config` 字段已赋值
3. 创建测试脚本调用 `SetInput`：
```csharp
void Update()
{
    CharacterInputData input = new CharacterInputData
    {
        MoveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")),
        JumpDown = Input.GetKeyDown(KeyCode.Space),
        CameraRotation = Camera.main.transform.rotation,
        UseCameraRotation = true
    };
    
    characterController.SetInput(ref input);
}

void Start()
{
    characterController.SetCamera(Camera.main);
}
```

#### 5.4 功能验证清单
- [ ] 地面移动流畅
- [ ] 角色朝向跟随移动方向
- [ ] 单次跳跃正常（高度合理）
- [ ] 多段跳正常（可配置次数）
- [ ] 土狼时间生效（离地后短时间内可跳跃）
- [ ] 跳跃缓冲生效（提前按跳跃也能起跳）
- [ ] 空中移动正常
- [ ] 碰到头顶提前结束跳跃
- [ ] 相机相对移动正常（角色朝相机前方移动）
- [ ] 重力和阻力正常

---

## 潜在问题与解决方案

### 问题1：跳跃高度不符合预期
**原因**: `JumpSpeed` 和 `JumpDuration` 配置不当
**解决**: 调整配置值，建议：
- 提高 `JumpSpeed` → 跳得更高
- 延长 `JumpDuration` → 跳得更高
- 调整曲线形状 → 改变跳跃感觉

### 问题2：空中移动感觉飘
**原因**: `AirAcceleration` 或 `AirDrag` 过小
**解决**: 
- 提高 `AirAcceleration` → 空中控制更灵敏
- 提高 `AirDrag` → 减少飘浮感

### 问题3：地面移动响应慢
**原因**: `StableMovementSharpness` 过小
**解决**: 提高 `StableMovementSharpness` 值（建议 15-30）

### 问题4：相机旋转不工作
**检查**:
1. 是否调用了 `SetCamera`？
2. `UseCameraRotation` 是否为 `true`？
3. `CameraRotation` 是否有效（不是 `Quaternion.identity`）？

### 问题5：跳跃曲线为 null
**解决**: 代码中的 `GetDefaultJumpCurve()` 方法会提供默认曲线，但建议在配置资源中明确设置。

---

## 后续优化建议

### 阶段2功能（可选）
如果后续需要，可以重新添加以下功能（单独实施）：
1. 飞扑系统（基于新的时间控制方式）
2. 蹲伏系统（简化实现）
3. 爬墙系统（可选）

### 性能优化
当前实现优先考虑清晰性，后续可优化：
1. 缓存更多计算结果
2. 减少 Vector3 计算
3. 使用对象池管理临时向量

### 动画集成
建议集成动画系统：
1. 根据 `_currentState` 切换动画
2. 根据 `_moveInputVector.magnitude` 控制移动动画速度
3. 根据 `_jumpState.IsActive` 播放跳跃动画

---

## 完成标准

以下所有条件满足即认为实施完成：

1. ✅ 所有代码编译通过，无错误
2. ✅ 所有配置参数可在 Inspector 正常显示和编辑
3. ✅ 功能验证清单全部通过
4. ✅ 没有 Unity Console 警告或错误
5. ✅ 代码符合命名规范和组织结构要求
6. ✅ 测试场景中角色行为符合预期

---

## 时间计划

- **配置修改**：30分钟
- **CharacterController 重构**：90分钟
- **测试与调试**：30分钟
- **文档更新**：10分钟

**总计**：约 2.5 小时

---

## 备注

- 本实施计划基于 `DesignDecisions.md` v2025-10-31 17:00 版本
- 如在实施过程中发现问题，请先更新决策文档再修改代码
- 建议使用版本控制（Git），在开始实施前创建新分支


