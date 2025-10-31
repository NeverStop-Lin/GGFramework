# 角色控制器优化 - 设计决策记录

## 项目目标
基于 DivCharacterController 优化 Character 文件夹中的角色控制器，保留所有功能但改进实现方式。

---

## 已确认的设计决策

### 1. 总体优化范围
- **决策**: 全面重构，保留所有功能但改进实现方式
- **范围**: 改进跳跃和移动部分
- **输入**: 添加相机旋转输入支持（像 DivCharacterController 的 CameraRotation）

### 2. 核心功能调整

#### 2.1 功能移除
- ✅ 移除飞扑（Dash）功能
- ✅ 移除爬墙（WallClimb）功能
- ✅ 移除梯子（Ladder）功能
- ✅ 移除蹲伏（Crouch）功能

#### 2.2 保留功能
- ✅ 基础移动（地面 + 空中）
- ✅ 多段跳跃系统
- ✅ 相机相对移动
- ✅ 土狼时间（Coyote Time）
- ✅ 跳跃缓冲（Jump Buffer）

### 3. 跳跃系统设计

#### 3.1 跳跃控制方式（最终方案）
- **方式**: 基于时间的跳跃控制 + AnimationCurve 速度曲线
- **核心机制**: 持续施加向上力（由曲线控制），同时受重力影响，最终高度自然形成
- **停止条件**: 当跳跃持续时间到达 `JumpDuration` 时停止施加力，进入自由落体
- **多段跳**: 每次跳跃（包括二段跳）都重置计时器，独立持续时间
- **物理特性**: 真实物理模拟，跳跃全程受重力影响

#### 3.2 跳跃配置参数（基于时间控制）
- **添加**: `JumpDuration`（跳跃持续时间，单位：秒）
- **添加**: `JumpSpeed`（基础跳跃速度，单位：m/s）
- **添加**: `JumpSpeedCurve`（AnimationCurve，X=时间进度0-1，Y=速度倍率0-1）
- **保留**: `MaxJumpCount`（最大跳跃次数，支持多段跳）
- **保留**: `CoyoteTime`（土狼时间）
- **保留**: `JumpBufferTime`（跳跃缓冲时间）
- **移除**: ~~`JumpHeight`~~（不再强制控制高度）
- **移除**: ~~`JumpUpSpeed`~~（被 `JumpSpeed` 替代）
- **移除**: ~~`InterruptJumpSpeed`~~（删除高级功能后不再需要打断机制）

#### 3.5 跳跃控制方案（重大调整）
- **方案**: ✅ 基于时间的跳跃控制（不强制控制高度）
- **核心思路**: 通过持续时间、曲线、力度共同决定跳跃高度（自然结果）
- **配置参数**:
  - `JumpDuration`（跳跃持续时间，如 0.5秒）
  - `JumpSpeed`（基础跳跃速度，如 10 m/s）
  - `JumpSpeedCurve`（速度曲线，X轴=时间进度0-1，Y轴=速度倍率0-1）
  - ~~移除 `JumpHeight`~~（不再强制控制高度）

#### 3.6 跳跃力施加方式
- **方式**: 添加增量（累积模式）
  ```csharp
  currentVelocity += CharacterUp * (JumpSpeed * speedMultiplier * deltaTime)
  ```
- **重力施加**: 跳跃期间也施加重力（自然物理，全程受重力影响）
- **曲线采样**: `progress = elapsedTime / JumpDuration`，`speedMultiplier = Curve.Evaluate(progress)`
- **结束条件**: 当 `progress >= 1.0` 时停止施加跳跃力，进入自由落体

#### 3.7 默认跳跃曲线
- **类型**: 平台跳曲线
- **关键帧**: `(0, 1.0) → (0.8, 0.8) → (1.0, 0)` - 前期保持速度，末期急停
- **特点**: 前80%时间保持较高速度，最后20%快速衰减
- **插值模式**: Smooth（平滑插值，Unity 自动计算切线）
- **空值处理**: 配置为 null 时，代码中提供上述默认曲线

#### 3.8 状态转换逻辑
- **进入 Jumping 状态**: 只要执行跳跃（`JumpDown` 且满足跳跃条件）就进入 `Jumping` 状态
- **适用范围**: 包括地面起跳、空中二段跳、三段跳等所有跳跃
- **退出 Jumping 状态**: 当 `ElapsedTime >= JumpDuration` 或被头顶截断时，转换到 `Airborne` 状态
- **着地处理**: `Airborne` 或 `Jumping` 状态着地后，转换到 `Default` 状态，重置跳跃计数

#### 3.9 跳跃状态管理（基于时间）
- **结构体**: 创建 `JumpState` 结构体封装所有跳跃相关状态
- **字段**:
  - `bool IsActive`（跳跃是否激活）
  - `float ElapsedTime`（已经过的时间）
  - `int ConsumedCount`（已消耗的跳跃次数）
  - `float CoyoteTimeRemaining`（剩余土狼时间）
  - `float BufferTimeRemaining`（剩余跳跃缓冲时间）
- **位置**: 定义在 CharacterController 类内部（private struct）
- **多段跳处理**: 每次跳跃重置 `ElapsedTime = 0`，独立持续时间

#### 3.10 土狼时间与跳跃缓冲
- **集成方式**: 计时器集成到 JumpState 结构体中
- **更新时机**: 
  - 着地时重置 `CoyoteTimeRemaining = Config.CoyoteTime`
  - 离地后每帧减少 `CoyoteTimeRemaining -= deltaTime`
  - 按下跳跃时设置 `BufferTimeRemaining = Config.JumpBufferTime`
  - 每帧减少 `BufferTimeRemaining -= deltaTime`
- **判定逻辑**: 
  - 地面跳：`IsGrounded || CoyoteTimeRemaining > 0`
  - 缓冲生效：`BufferTimeRemaining > 0`

#### 3.11 空中跳跃处理
- **垂直速度**: 保留部分垂直速度，叠加跳跃力（如果向上则保留）

#### 3.12 碰到头顶物体
- **检测方式**: 双重检测（速度截断 + 碰撞回调）
  1. **速度截断检测**：
     ```csharp
     float velocityBeforeJump = currentVelocity.y;
     // 施加跳跃力...
     // Motor处理后
     if (_jumpState.IsActive && currentVelocity.y < velocityBeforeJump - 0.1f) {
         _jumpState.IsActive = false;
     }
     ```
  2. **碰撞回调检测**（在 `OnMovementHit` 中）：
     ```csharp
     if (Vector3.Dot(hitNormal, Motor.CharacterUp) < -0.7f) {
         if (_jumpState.IsActive) {
             _jumpState.IsActive = false;
         }
     }
     ```
- **行为**: 任一检测触发后立即停止施加跳跃力，进入自由落体

#### 3.13 跳跃与移动的关系
- **水平控制**: 跳跃时允许全速空中移动，跳跃不影响水平控制
- **输入响应**: 跳跃期间保持完整的空中移动能力（`MaxAirMoveSpeed`）

### 4. 移动系统设计

#### 4.1 输入坐标转换
- **方式**: 采用 DivCharacterController 的相机平面投影方式（`cameraPlanarRotation * moveInputVector`）

#### 4.2 地面移动
- **实现**: 融合 DivCharacterController 和现有实现
  - 使用 DivCharacterController 的核心逻辑（Cross 计算切线）
  - 保留现有的速度 Lerp 平滑
- **速度平滑**: 混合方式 - 地面用 Lerp，旋转用 Slerp

#### 4.3 空中移动
- **方式**: 简化版本 + 保留防爬坡逻辑
- **基础实现**:
  ```csharp
  Vector3 targetVelocity = _moveInputVector * Config.MaxAirMoveSpeed;
  currentVelocity += (targetVelocity - currentVelocity) * Config.AirAcceleration * deltaTime;
  ```
- **防爬坡处理**（保留 DivCharacterController 的逻辑）:
  ```csharp
  if (Motor.GroundingStatus.FoundAnyGround) {
      Vector3 perpenticularObstructionNormal = 
          Vector3.Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal), 
                       Motor.CharacterUp).normalized;
      targetVelocity = Vector3.ProjectOnPlane(targetVelocity, perpenticularObstructionNormal);
  }
  ```
- **阻力**: Drag 只在空中生效（DivCharacterController 方式）
- **重力**: 统一使用 `Config.Gravity * deltaTime`（标准方式）

#### 4.4 速度平滑方式
- **地面状态**: 使用 `Vector3.Lerp` 平滑速度
  ```csharp
  currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, 
                                 1f - Mathf.Exp(-Config.StableMovementSharpness * deltaTime));
  ```
- **旋转插值**: 使用 `Vector3.Slerp` 球形插值（所有状态）
  ```csharp
  Vector3 smoothedLookDirection = Vector3.Slerp(Motor.CharacterForward, targetForward, 
                                                1f - Mathf.Exp(-Config.OrientationSharpness * deltaTime));
  ```

### 5. 状态机设计

#### 5.1 状态定义
- **保留状态**: `Default`, `Airborne`, `Jumping`
- **移除状态**: `Dash`, `Crouch`, `Ladder`, `WallClimb`
- **Jumping 状态**:
  - 进入: 只要执行跳跃就进入 Jumping 状态（包括多段跳）
  - 退出: 曲线结束（值为0）时转换到 Airborne

#### 5.2 状态机实现
- **方式**: 保持当前的简单 switch-case 状态机
- **代码组织**: 按功能分组（Input处理、跳跃逻辑、移动逻辑、状态转换等），使用 `#region`

### 6. 输入系统设计

#### 6.1 CharacterInputData 结构
- **保留字段**: `MoveInput` (Vector2), `JumpDown` (bool)
- **移除字段**: `DashDown`, `ToggleCrouchDown`
- **新增字段**: `Quaternion CameraRotation`（为 `Quaternion.identity` 时表示无相机旋转）
- **使用标志**: 添加 `bool useCameraRotation` 标志位

#### 6.2 CharacterInput 类
- **决策**: 删除 CharacterInput 类，只保留 CharacterInputData 结构体
- **使用方式**: 外部直接调用 `controller.SetInput(ref inputData)`
- **输入来源**: 外部输入，Controller 不自己获取

#### 6.3 相机引用管理
- **存储方式**: CharacterController 类中添加私有字段 `Camera _mainCamera`
- **设置方法**: 提供 `public void SetCamera(Camera camera)` 方法，外部通过此方法设置相机
- **获取时机**: Controller 内部每帧从存储的 `_mainCamera` 获取旋转
- **默认行为**: 启动时如果相机为 null 则报错（严格模式），运行时如果相机为 null 则使用 `Quaternion.identity`

#### 6.4 移动输入转换方式
- **方案**: 提供两种模式（灵活适配不同输入源）
- **模式1 - 原始输入**：
  - `SetInput` 接收原始 `Vector2 MoveInput`（-1到1）
  - 内部使用相机旋转转换为世界空间方向
  - 适合简单输入场景
- **模式2 - 世界空间输入**：
  - 外部已转换好世界空间方向，直接传入
  - `UseCameraRotation = false` 时跳过内部转换
  - 适合复杂输入系统
- **实现**: `CharacterInputData` 中的 `UseCameraRotation` 标志位控制

#### 6.5 CharacterInputData 最终定义
```csharp
public struct CharacterInputData
{
    public Vector2 MoveInput;         // 移动输入（-1到1）或世界空间方向
    public bool JumpDown;             // 跳跃按钮按下
    public Quaternion CameraRotation; // 相机旋转（可选）
    public bool UseCameraRotation;    // 是否使用相机旋转进行坐标转换
}
```

### 7. 配置参数调整

#### 7.1 CharacterControllerConfig 修改
- **删除参数**: 所有高级功能的配置参数（飞扑、爬墙、梯子、蹲伏相关）
- **新增参数**: 
  - `JumpDuration`（float，跳跃持续时间）
  - `JumpSpeed`（float，基础跳跃速度）
  - `JumpSpeedCurve`（AnimationCurve，速度曲线）
- **移除参数**: 
  - `JumpHeight`, `JumpUpSpeed`, `JumpForwardSpeed`（被新系统替代）
  - `InterruptJumpSpeed`（不再需要打断机制）
  - `DashSpeed`, `DashDuration`, `DashDecay`, `DashCooldown`, `AirDashConsumesJump`（删除飞扑）
  - `CrouchedHeight`, `StandingHeight`, `CapsuleRadius`, `CrouchedSpeedMultiplier`（删除蹲伏）
  - `LadderDetectionRadius`, `LadderDetectionDistance`, `LadderClimbSpeed`, `LadderTag`, `LadderLayer`（删除梯子）
  - `ClimbableWallLayer`, `MaxWallClimbAngle`, `WallStickDistance`, `WallSlideSpeed`, `MaxWallGrabDuration`, `WallJumpUpSpeed`, `WallJumpBackSpeed`（删除爬墙）
- **保留参数**: 
  - 基础移动：`MaxStableMoveSpeed`, `StableMovementSharpness`, `OrientationSharpness`
  - 空中移动：`MaxAirMoveSpeed`, `AirAcceleration`, `AirDrag`, `Gravity`
  - 跳跃系统：`MaxJumpCount`, `CoyoteTime`, `JumpBufferTime`

#### 7.2 推荐默认配置值
- **跳跃系统**:
  - `JumpDuration = 0.4f`（跳跃持续时间，适中值）
  - `JumpSpeed = 12f`（基础跳跃速度，配合默认曲线和重力）
  - `MaxJumpCount = 2`（二段跳）
  - `CoyoteTime = 0.12f`（土狼时间）
  - `JumpBufferTime = 0.15f`（跳跃缓冲）
- **基础移动**:
  - `MaxStableMoveSpeed = 10f`
  - `StableMovementSharpness = 15f`
  - `OrientationSharpness = 10f`
- **空中移动**:
  - `MaxAirMoveSpeed = 10f`
  - `AirAcceleration = 15f`
  - `AirDrag = 0.1f`
  - `Gravity = new Vector3(0, -30f, 0)`

### 8. 代码风格与组织

#### 8.1 命名规范
- **风格**: 统一使用下划线前缀私有字段（`_jumpState`, `_currentVelocity`）

#### 8.2 代码分组
- **Region 划分**:
  - `#region Configuration`
  - `#region State`
  - `#region Motor Interface`
  - `#region Internal Logic`

---

## 待决策问题

### 🔄 无待决策问题
所有核心设计决策已完成，可以开始实施。

如有新问题会在此继续添加。

---

## 文件修改清单

### 需要修改的文件
- ✅ `CharacterControllerConfig.cs` - 删除高级功能配置，添加跳跃曲线配置
- ✅ `CharacterController.cs` - 重构核心逻辑
- ✅ `CharacterState.cs` - 简化状态枚举
- ⚠️ `CharacterInput.cs` - 删除此文件

### 需要创建的文件
- 无（JumpState 定义在 CharacterController 内部）

---

## 更新日志
- 2025-10-31 15:00: 初始创建，记录所有已确认的设计决策
- 2025-10-31 16:30: 重大调整 - 从基于高度控制改为基于时间控制的跳跃系统
  - 移除 `JumpHeight` 参数
  - 添加 `JumpDuration` 参数
  - 跳跃全程受重力影响（真实物理）
  - 高度由时间、速度、曲线、重力共同决定（自然结果）
  - JumpState 字段调整：移除 `AccumulatedHeight` 和 `StartPosition`，添加 `ElapsedTime`
- 2025-10-31 17:00: 完成所有实现细节决策
  - 移除 `InterruptJumpSpeed`（不再需要打断机制）
  - JumpState 集成土狼时间和跳跃缓冲计时器（`CoyoteTimeRemaining`, `BufferTimeRemaining`）
  - 确认默认曲线：平台跳曲线 `(0, 1.0) → (0.8, 0.8) → (1.0, 0)`，Smooth插值
  - 确认头顶碰撞检测：双重检测（速度截断 + OnMovementHit回调）
  - 确认状态转换：所有跳跃进入 `Jumping` 状态
  - 确认跳跃不影响水平控制（全速空中移动）
  - 相机管理：Controller 存储相机引用，内部获取旋转
  - 输入转换：支持两种模式（原始输入 + 已转换输入）
  - 空中移动：简化实现 + 保留防爬坡逻辑
  - 速度平滑：地面用 Lerp，旋转用 Slerp
  - 确定推荐默认配置值

