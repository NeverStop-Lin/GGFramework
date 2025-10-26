# UI系统 - OnDestroy vs OnRemove 设计说明

**日期**: 2025-10-26  
**版本**: MonoBehaviour架构

---

## 📖 命名演变

### 方案演变史

```
v1: OnDestroy() + OnDestroy(params object[] args)
    ❌ 同名方法，C#允许重载，但容易混淆

v2: OnDestroy() + OnUIDestroy(params object[] args)  
    ⚠️ 名字不同了，但都带"Destroy"，还是容易搞混

v3: OnDestroy() + OnRemove(params object[] args)
    ✅ 名字完全不同，语义清晰，命名对称
```

---

## 🎯 核心问题

在MonoBehaviour UI架构中，存在**两个销毁相关的方法**，容易引起混淆：

1. **Unity的`OnDestroy()`** - GameObject销毁时的生命周期钩子
2. **UI业务的移除回调** - UI系统移除时的业务逻辑处理

---

## 🏗️ 设计方案

### 方法命名

| 方法名 | 参数 | 调用时机 | 用途 | 访问级别 |
|--------|------|----------|------|----------|
| `OnDestroy()` | 无参 | GameObject被销毁时 | Unity生命周期钩子 | `protected virtual` |
| `OnRemove(params object[] args)` | 有参 | UI移除Pipeline执行时 | 业务逻辑清理 | `protected virtual` |

### 为什么这样设计？

#### 1. **完全避免命名冲突** ✅

**之前的设计（有问题）**:
```csharp
// ❌ 两个同名方法，容易混淆
protected virtual void OnDestroy()           // Unity钩子
protected virtual void OnDestroy(params object[] args)  // 业务回调
```

**改进的设计（还是有"Destroy"）**:
```csharp
// ⚠️ 名字虽然不同，但还是容易混淆
protected virtual void OnDestroy()           // Unity钩子
protected virtual void OnUIDestroy(params object[] args)  // 业务回调
```

**最终设计（完美）**:
```csharp
// ✅ 名字完全不同，清晰明了
protected virtual void OnDestroy()           // Unity钩子
protected virtual void OnRemove(params object[] args)  // 业务回调
```

#### 2. **语义更准确** ✅

- `OnDestroy` → Unity的销毁钩子（GameObject被销毁）
- `OnRemove` → UI的移除回调（UI被移除/卸载）

从语义上讲，UI被"移除"比被"销毁"更准确，因为：
- GameObject可能被缓存而不是真正销毁
- UI可能被隐藏后重用
- "Remove"强调的是从UI系统中移除，而不是物理销毁

#### 3. **清晰的职责分离** ✅

```csharp
/// <summary>
/// Unity OnDestroy钩子（GameObject销毁时）
/// 自动触发Destroy Pipeline
/// </summary>
protected virtual void OnDestroy()
{
    // 职责：确保Pipeline被执行
    if (uiState == UIState.Destroy) return;
    _ = DoDestroy();
}

/// <summary>
/// UI移除时调用（Pipeline回调）
/// 子类可以重写此方法来清理资源和执行业务逻辑
/// </summary>
protected virtual void OnRemove(params object[] args)
{
    // 职责：清理资源，执行业务逻辑
    Canvas = null;
    RectTransform = null;
    FrameworkLogger.Info($"[UI] UI销毁: {GetType().Name}");
}
```

#### 4. **子类使用更明确** ✅

**子类重写示例**:
```csharp
public class MainMenuUI : UIBehaviour
{
    // ✅ 清晰明了 - 重写UI移除回调
    protected override void OnRemove(params object[] args)
    {
        // 清理MainMenuUI的资源
        // 保存数据等
        
        base.OnRemove(args);
    }
    
    // ⚠️ 通常不需要重写Unity的OnDestroy
    // 除非你有特殊需求
    protected override void OnDestroy()
    {
        base.OnDestroy();
    }
}
```

#### 5. **命名对称性** ✅

生命周期方法命名更协调：
```
OnCreate  → 创建
OnShow    → 显示
OnReady   → 就绪
OnHide    → 隐藏
OnRemove  → 移除  ✅ 简洁对称
```

vs 旧方案：
```
OnCreate     → 创建
OnShow       → 显示
OnReady      → 就绪
OnHide       → 隐藏
OnUIDestroy  → 销毁  ⚠️ 不对称，太长
```

---

## 🔄 执行流程

### 正常销毁流程（通过UI系统）

```
用户代码调用：Center.Hide<MainMenuUI>()
         ↓
UICenter.Hide() → DoDestroy()
         ↓
UIBehaviour.DoDestroy(args)
         ↓
执行Destroy Pipeline
         ↓
SelfAttachmentAdapter.OnBeforeDestroy
         ↓
UIBehaviour.OnRemove(args)  ← 业务回调（子类可重写）
         ↓
Destroy(gameObject)
         ↓
Unity: OnDestroy()  ← Unity钩子（自动调用）
         ↓
检测到uiState == UIState.Destroy，直接返回（避免重复执行）
```

### 异常销毁流程（GameObject被直接销毁）

```
外部代码：Destroy(gameObject) 或 场景卸载
         ↓
Unity: OnDestroy()  ← Unity钩子（自动调用）
         ↓
检测到uiState != UIState.Destroy
         ↓
执行：_ = DoDestroy()  ← 触发Pipeline
         ↓
执行Destroy Pipeline
         ↓
UIBehaviour.OnRemove(args)  ← 业务回调
         ↓
Destroy(gameObject)  ← 已经在销毁了，不会重复
```

---

## 📋 完整代码示例

### 基类实现

```csharp
public abstract class UIBehaviour : MonoBehaviour, IBaseUI
{
    // ==================== Unity生命周期 ====================
    
    /// <summary>
    /// Unity OnDestroy钩子（GameObject销毁时）
    /// 自动触发Destroy Pipeline
    /// </summary>
    protected virtual void OnDestroy()
    {
        // 如果是通过DoDestroy销毁的，不要重复执行
        if (uiState == UIState.Destroy)
        {
            return;
        }
        
        // Unity直接销毁GameObject时，触发Destroy Pipeline
        _ = DoDestroy();
    }
    
    // ==================== Pipeline接口 ====================
    
    /// <summary>
    /// 执行Destroy Pipeline
    /// </summary>
    public async Task<object> DoDestroy(params object[] args)
    {
        var result = await ExecuteStatePipelineAsync(UIState.Destroy, args);
        
        // 销毁GameObject
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
        
        return result;
    }
    
    // ==================== 业务回调 ====================
    
    /// <summary>
    /// UI移除时调用（Pipeline回调）
    /// 子类可以重写此方法来清理资源和执行业务逻辑
    /// </summary>
    protected virtual void OnRemove(params object[] args)
    {
        // 清理资源
        Canvas = null;
        RectTransform = null;
        
        FrameworkLogger.Info($"[UI] UI销毁: {GetType().Name}");
    }
    
    // ==================== Pipeline适配器 ====================
    
    private class SelfAttachmentAdapter : UIAttachment
    {
        private readonly UIBehaviour _ui;
        
        protected override Task OnBeforeDestroy(PipelineContext context)
        {
            _ui.uiState = UIState.Destroy;
            _ui.OnRemove(GetParameters(context));  // ← 调用业务回调
            return Task.CompletedTask;
        }
    }
}
```

### 子类使用示例

```csharp
public class MainMenuUI : UIBehaviour
{
    private SoundPlayer _bgMusic;
    private List<GameObject> _tempObjects;
    
    /// <summary>
    /// UI显示时调用
    /// </summary>
    protected override void OnShow(params object[] args)
    {
        base.OnShow(args);
        
        // 播放背景音乐
        _bgMusic = SoundManager.PlayBGM("MenuMusic");
        
        // 创建一些临时对象
        _tempObjects = new List<GameObject>();
    }
    
    /// <summary>
    /// UI移除时调用（清理资源）
    /// </summary>
    protected override void OnRemove(params object[] args)
    {
        // 停止背景音乐
        if (_bgMusic != null)
        {
            _bgMusic.Stop();
            _bgMusic = null;
        }
        
        // 清理临时对象
        if (_tempObjects != null)
        {
            foreach (var obj in _tempObjects)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
            _tempObjects.Clear();
            _tempObjects = null;
        }
        
        // 调用基类清理
        base.OnRemove(args);
    }
    
    // ⚠️ 通常不需要重写Unity的OnDestroy
    // 基类已经正确处理了所有情况
}
```

---

## ⚙️ Pipeline配置

在UIAttachment中，仍然使用`OnDestroy`作为方法名，因为：
1. UIAttachment不继承自MonoBehaviour，没有命名冲突
2. 它是Pipeline中间件，不是生命周期钩子

```csharp
// UIAttachment.cs
public abstract class UIAttachment
{
    /// <summary>
    /// 处理UI销毁事件
    /// </summary>
    public async Task OnDestroy(PipelineContext context, Func<Task> next)
    {
        _curContext = context;
        await OnBeforeDestroy(context);
        await next();
        await OnAfterDestroy(context);
    }
    
    protected virtual Task OnBeforeDestroy(PipelineContext context) 
    {
        return Task.CompletedTask;
    }
    
    protected virtual Task OnAfterDestroy(PipelineContext context) 
    {
        return Task.CompletedTask;
    }
}

// UIBehaviour.cs - Pipeline配置
var stateConfigurations = new[]
{
    (UIState.Create, a => a.OnCreate),
    (UIState.Show, a => a.OnShow),
    (UIState.Ready, a => a.OnReady),
    (UIState.Hide, a => a.OnHide),
    (UIState.Destroy, a => a.OnDestroy)  // ← 调用UIAttachment.OnDestroy
};
```

---

## ✅ 设计优势总结

### 1. 命名清晰 ✅
- `OnDestroy()` - 一看就知道是Unity钩子
- `OnRemove(params object[] args)` - 一看就知道是UI业务回调

### 2. 职责分离 ✅
- `OnDestroy()` - 确保Pipeline被执行
- `OnRemove()` - 清理资源和业务逻辑

### 3. 防呆设计 ✅
- 无论如何销毁GameObject，都能正确执行清理逻辑
- 避免重复执行（通过uiState检测）

### 4. 子类友好 ✅
- 开发者只需重写`OnRemove()`即可
- 不需要关心复杂的Pipeline和Unity生命周期

### 5. 可测试性 ✅
- 可以直接调用`DoDestroy()`测试销毁逻辑
- 不依赖Unity的GameObject销毁

---

## ⚠️ 常见错误

### ❌ 错误1：混淆两个方法

```csharp
// ❌ 错误：重写了Unity的OnDestroy，而不是OnRemove
protected override void OnDestroy()
{
    // 清理资源... 这会导致Pipeline没有正确执行！
    base.OnDestroy();
}
```

**正确做法**:
```csharp
// ✅ 正确：重写OnRemove
protected override void OnRemove(params object[] args)
{
    // 清理资源
    base.OnRemove(args);
}
```

### ❌ 错误2：不调用base方法

```csharp
// ❌ 错误：没有调用base.OnRemove
protected override void OnRemove(params object[] args)
{
    // 清理资源...
    // 忘记调用base了！
}
```

**正确做法**:
```csharp
// ✅ 正确：始终调用base
protected override void OnRemove(params object[] args)
{
    // 清理资源...
    
    base.OnRemove(args);  // ← 别忘了！
}
```

### ❌ 错误3：在OnDestroy中做业务逻辑

```csharp
// ❌ 错误：在Unity钩子中做业务逻辑
protected override void OnDestroy()
{
    // 清理资源...
    StopMusic();
    SaveData();
    
    base.OnDestroy();
}
```

**正确做法**:
```csharp
// ✅ 正确：在OnRemove中做业务逻辑
protected override void OnRemove(params object[] args)
{
    // 清理资源...
    StopMusic();
    SaveData();
    
    base.OnRemove(args);
}
```

---

## 📊 方法对比表

| 特性 | OnDestroy() | OnRemove(params object[] args) |
|------|-------------|-----------------------------------|
| **来源** | Unity MonoBehaviour | UI系统设计 |
| **调用者** | Unity引擎 | Pipeline系统 |
| **参数** | 无参 | 可变参数 |
| **调用时机** | GameObject被销毁时 | UI销毁Pipeline执行时 |
| **重写频率** | 很少 | 经常 |
| **主要职责** | 触发Pipeline | 清理资源和业务逻辑 |
| **是否可以不调用** | 不能（Unity会自动调用） | 可以（但不推荐） |

---

## 🎯 最佳实践

### 推荐做法 ✅

1. **只重写OnRemove** - 99%的情况下，你只需要重写这个
2. **始终调用base** - 确保基类的清理逻辑执行
3. **清理顺序** - 先清理子类资源，再调用base
4. **null检查** - 清理前检查对象是否为null
5. **异常处理** - 清理逻辑中捕获异常，避免影响其他清理

### 完整示例 ✅

```csharp
public class GameUI : UIBehaviour
{
    private Timer _timer;
    private List<Enemy> _enemies;
    private AudioSource _bgm;
    
    protected override void OnRemove(params object[] args)
    {
        try
        {
            // 1. 停止计时器
            if (_timer != null)
            {
                _timer.Stop();
                _timer = null;
            }
            
            // 2. 清理敌人列表
            if (_enemies != null)
            {
                foreach (var enemy in _enemies)
                {
                    if (enemy != null)
                    {
                        enemy.Dispose();
                    }
                }
                _enemies.Clear();
                _enemies = null;
            }
            
            // 3. 停止音乐
            if (_bgm != null)
            {
                _bgm.Stop();
                _bgm = null;
            }
        }
        catch (Exception ex)
        {
            FrameworkLogger.Error($"[GameUI] 清理资源失败: {ex.Message}");
        }
        finally
        {
            // 4. 始终调用base
            base.OnRemove(args);
        }
    }
}
```

---

## 📝 总结

### 设计原则

1. **命名清晰** - 不同用途使用不同名字
2. **职责单一** - 每个方法只做一件事
3. **防呆设计** - 任何情况下都能正确执行
4. **子类友好** - 简单易用，不易出错

### 记住这个口诀 🎯

```
OnDestroy()  → Unity调用，触发Pipeline
OnRemove()   → 我来重写，清理资源
```

**就这么简单！** ✨

