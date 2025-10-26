# UI多实例功能使用指南

## 📋 概述

UI多实例功能允许同一个UI类同时创建和显示多个实例，适用于以下场景：
- 多个对话框
- 多个浮动窗口
- 多个物品详情页
- 聊天气泡
- 提示消息等

## 🎯 核心特性

### 1. 实例策略

UI系统支持两种实例策略：

- **单例模式（Singleton）**：同一时间只能存在一个实例（默认）
- **多实例模式（Multiple）**：可以同时存在多个实例

### 2. 配置方式

在UI管理器中配置每个UI的实例策略：

```
Tools/Framework/UI Manager -> UI管理 -> 实例策略下拉框
```

- 🟢 **单例**：传统的单一实例模式
- 🔵 **多实例**：支持多个实例同时存在

## 💻 代码示例

### 单例模式（默认行为）

```csharp
// 配置为单例模式的UI，多次调用Show会复用同一个实例，并自动刷新层级（置顶）
Center.Show<MainMenuUI>();
Center.Show<MainMenuUI>(); // 不会创建新实例，复用已有实例并置顶
```

### 多实例模式

```csharp
// 配置为多实例模式的UI，可以同时显示多个

// 🎯 推荐方式：自动生成实例ID（无需手动管理）
Center.Show<DialogUI>("消息1");
Center.Show<DialogUI>("消息2");
Center.Show<DialogUI>("消息3");
// 每次调用都会自动创建新实例，使用内部自动生成的ID（__auto__1, __auto__2...）

// 方式2：手动指定实例ID（需要精确控制时使用）
Center.Show<ChatBubbleUI>("player_123", "你好！");
Center.Show<ChatBubbleUI>("player_456", "Hi！");

// 隐藏指定实例（手动ID）
await Center.Hide<ChatBubbleUI>("player_123");

// 销毁指定实例（手动ID）
await Center.DestroyUI<ChatBubbleUI>("player_456");

// 销毁该类型的所有实例（包括自动和手动ID）
await Center.DestroyAllInstancesOf<DialogUI>();
```

### 实际应用场景

#### 场景1：聊天气泡

```csharp
public class ChatManager : MonoBehaviour
{
    [Inject] private IUI _uiCenter;
    
    public void ShowChatBubble(string playerId, string message)
    {
        // 每个玩家一个聊天气泡
        _uiCenter.Show<ChatBubbleUI>(playerId, message);
        
        // 3秒后自动隐藏
        _ = HideChatBubbleAfterDelay(playerId, 3f);
    }
    
    private async Task HideChatBubbleAfterDelay(string instanceId, float delay)
    {
        await Task.Delay((int)(delay * 1000));
        await _uiCenter.Hide<ChatBubbleUI>(instanceId);
    }
    
    // 清除所有聊天气泡
    public async Task ClearAllChatBubbles()
    {
        await _uiCenter.DestroyAllInstancesOf<ChatBubbleUI>();
    }
}
```

#### 场景2：物品详情窗口

```csharp
public class InventoryManager : MonoBehaviour
{
    [Inject] private IUI _uiCenter;
    
    public void ShowItemDetails(int itemId)
    {
        // 可以同时打开多个物品详情
        var instanceId = $"item_{itemId}";
        _uiCenter.Show<ItemDetailsUI>(instanceId, itemId);
    }
}
```

#### 场景3：浮动提示（推荐：自动实例ID）

```csharp
public class ToastManager : MonoBehaviour
{
    [Inject] private IUI _uiCenter;
    
    public void ShowToast(string message)
    {
        // 🎯 简化方式：自动生成实例ID
        _uiCenter.Show<ToastUI>(message);
        // 系统会自动创建新实例，无需手动管理ID
        // 隐藏时配合 NeverCache 策略自动销毁
    }
    
    // 清除所有提示（例如切换场景时）
    public async Task ClearAllToasts()
    {
        await _uiCenter.DestroyAllInstancesOf<ToastUI>();
    }
}
```

#### 场景4：需要精确控制的情况（手动实例ID）

```csharp
public class PlayerInfoManager : MonoBehaviour
{
    [Inject] private IUI _uiCenter;
    
    public void ShowPlayerInfo(string playerId, PlayerData data)
    {
        // 使用玩家ID作为实例ID，便于后续管理
        _uiCenter.Show<PlayerInfoUI>(playerId, data);
    }
    
    public async Task HidePlayerInfo(string playerId)
    {
        // 精确隐藏指定玩家的信息窗口
        await _uiCenter.Hide<PlayerInfoUI>(playerId);
    }
}
```

## ⚙️ 配置步骤

### 1. 打开UI管理器

```
Tools/Framework/UI Manager
```

### 2. 配置实例策略

1. 在"UI管理"Tab中找到要配置的UI
2. 在"实例策略"列选择：
   - **单例**：同一时间只能显示一个
   - **多实例**：可以同时显示多个
3. 点击"更新"按钮保存配置

### 3. 使用多实例

配置为多实例后，在代码中使用：

```csharp
// 带实例ID的Show方法
Center.Show<YourUI>("instance_id", args);

// 带实例ID的Hide方法
Center.Hide<YourUI>("instance_id");

// 带实例ID的DestroyUI方法
Center.DestroyUI<YourUI>("instance_id");
```

## 📝 注意事项

### 1. 自动实例ID vs 手动实例ID

**自动实例ID（推荐）：**
- 多实例UI调用 `Show<T>(args)` 时自动生成
- 格式：`__auto__1`, `__auto__2`, `__auto__3`...
- 优点：无需手动管理，简单易用
- 适用场景：对话框、提示、临时UI

**手动实例ID：**
- 调用 `Show<T>(instanceId, args)` 手动指定
- 格式：自定义（如 "player_123", "item_456"）
- 优点：精确控制，便于业务逻辑关联
- 适用场景：聊天气泡、玩家信息窗口等需要关联业务数据的UI

**ID冲突避免：**
- 自动ID使用 `__auto__` 前缀
- 手动ID建议避免使用该前缀
- 如果需要精确控制，请全部使用手动ID

### 2. 单例模式的层级刷新

- 单例UI多次调用Show会**自动刷新层级（置顶）**
- 确保单例UI总是显示在最上层
- 适合主界面、设置界面等常驻UI

```csharp
// 单例UI会自动置顶
Center.Show<SettingsUI>();  // 第一次显示
// ... 打开其他UI ...
Center.Show<SettingsUI>();  // 不创建新实例，但会置顶
```

### 3. 预加载行为

**重要：PreloadUI只加载资源，不执行业务逻辑**

`PreloadUI<T>()` 的工作原理：
- ✅ 使用框架的 `IResource.LoadAsync<GameObject>()` 加载Prefab
- ✅ **不会实例化GameObject**
- ✅ **不会触发Awake、OnEnable、OnCreate等生命周期**
- ✅ **不会执行UI脚本的业务逻辑**
- ✅ 框架资源系统会缓存和管理Prefab，后续实例化时直接使用

**框架资源系统的优势：**
- 统一的资源加载和缓存管理
- 支持引用计数，自动释放未使用的资源
- 支持Addressables等多种加载方式
- 可追踪和监控资源加载状态

**适用场景：**
- 单例UI和多实例UI都可以安全预加载
- 在加载界面、启动流程中预加载常用UI
- 减少首次显示时的资源加载时间

```csharp
// 预加载常用UI（安全，不会执行业务逻辑）
await Center.PreloadUI<MainMenuUI>();
await Center.PreloadUI<DialogUI>();
await Center.PreloadUI<SettingsUI>();

// 批量预加载（推荐）
await Task.WhenAll(
    Center.PreloadUI<MainMenuUI>(),
    Center.PreloadUI<DialogUI>(),
    Center.PreloadUI<SettingsUI>()
);
```

### 4. 内存管理

- 多实例模式下，每个实例都会占用内存
- 不再使用的实例应及时销毁
- 批量清理：使用 `DestroyAllInstancesOf<T>()` 销毁所有实例
- 建议配合缓存策略使用：
  - `NeverCache`：隐藏时自动销毁
  - `AlwaysCache`：手动控制销毁
  
**典型场景：**
```csharp
// 切换场景前清理所有对话框
await Center.DestroyAllInstancesOf<DialogUI>();

// 清理所有临时提示
await Center.DestroyAllInstancesOf<ToastUI>();
```

### 5. 单例模式下的行为

- 即使传入了实例ID，单例模式会忽略它
- 始终返回同一个实例

```csharp
// SingletonUI配置为单例模式
Center.Show<SingletonUI>("id1");
Center.Show<SingletonUI>("id2"); // 忽略id2，返回同一实例
```

### 6. 性能考虑

- 多实例模式适合少量实例（< 10个）
- 大量实例建议使用对象池模式
- 复杂UI建议使用单例+数据更新模式

## 🔄 API完整列表

### Show方法

```csharp
// 单例或默认实例
UiLifeCycle<T> Show<T>(params object[] args)

// 多实例
UiLifeCycle<T> Show<T>(string instanceId, params object[] args)
```

### Hide方法

```csharp
// 单例或默认实例
Task<object> Hide<T>(params object[] args)

// 多实例
Task<object> Hide<T>(string instanceId, params object[] args)
```

### DestroyUI方法

```csharp
// 销毁单例或默认实例
Task DestroyUI<T>()

// 销毁指定实例
Task DestroyUI<T>(string instanceId)

// 销毁该类型的所有实例
Task DestroyAllInstancesOf<T>()
```

### PreloadUI方法

```csharp
// 预加载UI资源（只加载Prefab，不实例化）
Task PreloadUI<T>()
```

**预加载行为说明：**
- 使用框架资源系统加载Prefab（`IResource.LoadAsync`）
- 不实例化GameObject，不执行任何生命周期
- 单例UI和多实例UI行为一致
- 框架会管理资源缓存和引用计数
- 后续Show时直接使用缓存的Prefab，加载更快

## 🎨 UI实例键格式

系统内部使用 `UIInstanceKey` 结构来唯一标识每个UI实例：

```csharp
// 日志输出格式
[UICenter] 请求显示UI: DialogUI#instance1
[UICenter] 请求显示UI: ChatBubbleUI#player_123
[UICenter] 请求显示UI: MainMenuUI  // 单例模式，无实例ID
```

## 🚀 最佳实践

1. **合理选择实例策略**
   - 主界面、设置界面 → 单例
   - 对话框、提示、气泡 → 多实例

2. **多实例使用建议**
   - 优先使用自动实例ID（`Show<T>(args)`）
   - 只在需要精确控制时使用手动ID
   - 配合 `NeverCache` 策略自动清理

3. **单例UI使用建议**
   - 利用自动置顶特性，无需手动管理层级
   - 适合常驻UI，减少创建销毁开销

4. **生命周期管理**
   - 临时UI使用 `NeverCache` 自动销毁
   - 批量清理使用 `DestroyAllInstancesOf<T>()`
   - 场景切换前清理所有实例

5. **代码组织**
   - 封装多实例管理逻辑
   - 避免硬编码实例ID
   - 提供便捷的辅助方法

## 📚 相关文档

- [UI系统概述](./UI系统概述.md)
- [UI最佳实践](./UI最佳实践.md)
- [开发扩展编码规范](./开发扩展编码规范.md)

