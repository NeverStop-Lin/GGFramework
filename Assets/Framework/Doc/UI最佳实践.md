# UI最佳实践

> **版本**: v2.0  
> **更新日期**: 2025-01-26

---

## 📋 目录

1. [UI设计最佳实践](#1-ui设计最佳实践)
2. [代码编写最佳实践](#2-代码编写最佳实践)
3. [性能优化](#3-性能优化)
4. [常见坑点](#4-常见坑点)
5. [调试技巧](#5-调试技巧)

---

## 1. UI设计最佳实践

### 1.1 组件层级设计

**推荐结构**：
```
Canvas（带Canvas组件）
  └─ Panel（RectTransform）
      ├─ Header
      │   └─ @Text_Title
      ├─ Content
      │   ├─ @Image_Icon
      │   └─ @Text_Description
      └─ Buttons
          ├─ @Button_Confirm
          └─ @Button_Cancel
```

**优点**：
- 结构清晰
- 易于查找组件
- 路径简短

### 1.2 Canvas设置

**推荐配置**：
- Render Mode: `Screen Space - Overlay`
- Canvas Scaler: `Scale With Screen Size`
- Reference Resolution: `1920 x 1080`

### 1.3 哪些组件需要标记？

**需要标记（需要在代码中访问）**：
- ✅ 需要动态修改的Text
- ✅ 所有Button
- ✅ 需要动态修改的Image
- ✅ InputField、Toggle、Slider等交互组件
- ✅ 需要动态控制的GameObject

**不需要标记（纯展示）**：
- ❌ 静态的装饰性Image
- ❌ 固定不变的Text
- ❌ 纯美术的容器GameObject

---

## 2. 代码编写最佳实践

### 2.1 生命周期使用

```csharp
public partial class MainMenuUI
{
    // ✅ OnShow：初始化数据，绑定Observer
    protected override void OnShow(params object[] args)
    {
        base.OnShow(args);
        
        // 解析参数
        if (args.Length > 0 && args[0] is int level)
        {
            _levelText.text = $"等级: {level}";
        }
        
        // 绑定数据
        _goldObserver = GridFramework.Observer.Cache("gold", 0);
        _goldText.BindNumber(_goldObserver, "金币: {0}");
    }
    
    // ✅ OnHide：清理临时状态，但不销毁数据
    protected override void OnHide(params object[] args)
    {
        // 清理临时状态
        _tempData = null;
        
        base.OnHide(args);
    }
    
    // ✅ OnDestroy：释放资源，取消订阅
    protected override void OnDestroy(params object[] args)
    {
        // 清理Observer订阅
        _goldObserver?.OnChange.Clear();
        
        base.OnDestroy(args);
    }
}
```

### 2.2 事件处理

```csharp
// ✅ 清晰的事件处理
private void OnBuyItemClick()
{
    // 1. 验证
    if (_goldObserver.Value < itemPrice)
    {
        ShowToast("金币不足");
        return;
    }
    
    // 2. 执行业务逻辑
    _goldObserver.Value -= itemPrice;
    AddItemToInventory(itemId);
    
    // 3. UI反馈
    PlayBuyAnimation();
    ShowToast("购买成功");
}

// ❌ 避免在事件中写过多逻辑
private void OnBuyItemClick()
{
    // 不好：所有逻辑堆在一起
    if (gold < price && inventory.Count < max && time > 0 && ...)
    {
        // 100行代码...
    }
}
```

### 2.3 参数传递

```csharp
// ✅ 显示UI时传递参数
GridFramework.UI.Show<ShopUI>(itemId, itemPrice);

// 在UI中接收
protected override void OnShow(params object[] args)
{
    base.OnShow(args);
    
    if (args.Length >= 2)
    {
        var itemId = (int)args[0];
        var itemPrice = (int)args[1];
        
        LoadItemData(itemId, itemPrice);
    }
}
```

---

## 3. 性能优化

### 3.1 合理使用缓存策略

```csharp
// ✅ 频繁打开的UI：总是缓存
protected override UIConfig CreateUIConfig()
{
    return new UIConfig
    {
        CacheStrategy = UICacheStrategy.AlwaysCache
    };
}

// ✅ 偶尔打开的大UI：不缓存
protected override UIConfig CreateUIConfig()
{
    return new UIConfig
    {
        CacheStrategy = UICacheStrategy.NeverCache
    };
}
```

### 3.2 预加载关键UI

```csharp
// 启动时预加载
async void Start()
{
    await GridFramework.UI.PreloadUI<MainMenuUI>();
    await GridFramework.UI.PreloadUI<LoadingUI>();
}
```

或在UIManifest中配置：
```
MainMenuUI:
  Preload: true
```

### 3.3 避免频繁创建销毁

```csharp
// ❌ 不好：每次都销毁重建
CacheStrategy = UICacheStrategy.NeverCache

// ✅ 好：缓存复用
CacheStrategy = UICacheStrategy.AlwaysCache
```

### 3.4 使用对象池

```csharp
// 对于列表项等重复UI
var pool = GridFramework.Pool.CreateGameObjectPool(itemPrefab, 10);

foreach (var data in items)
{
    var item = pool.Spawn();
    item.GetComponent<ItemUI>().SetData(data);
}
```

---

## 4. 常见坑点

### 坑点1：在构造函数中访问组件

```csharp
// ❌ 错误：组件还未绑定
public MainMenuUI()
{
    _titleText.text = "标题";  // NullReferenceException!
}

// ✅ 正确：在OnShow中访问
protected override void OnShow(params object[] args)
{
    base.OnShow(args);
    _titleText.text = "标题";
}
```

### 坑点2：忘记调用base方法

```csharp
// ❌ 错误：没有调用base
protected override void OnShow(params object[] args)
{
    _titleText.text = "标题";
    // 忘记调用base.OnShow(args)，导致GameObject不会SetActive(true)
}

// ✅ 正确
protected override void OnShow(params object[] args)
{
    base.OnShow(args);
    _titleText.text = "标题";
}
```

### 坑点3：手动修改Binding.cs

```csharp
// ❌ 错误：在Binding.cs中添加逻辑
// MainMenuUI.Binding.cs
protected override void RegisterEvents()
{
    base.RegisterEvents();
    _startButton.onClick.AddListener(OnStartClick);
    
    // 添加了自定义逻辑，重新生成时会丢失！
    _startButton.interactable = false;
}

// ✅ 正确：在Logic.cs中添加
// MainMenuUI.cs
protected override void OnShow(params object[] args)
{
    base.OnShow(args);
    
    // 在这里写自定义逻辑
    _startButton.interactable = false;
}
```

### 坑点4：异步方法不等待

```csharp
// ❌ 错误：不等待异步方法
private void OnStartClick()
{
    LoadData();  // 异步方法，没有等待
    Show UI();   // 可能在数据加载完成前就显示了
}

// ✅ 正确：使用async/await
private async void OnStartClick()
{
    await LoadData();
    ShowUI();
}
```

---

## 5. 调试技巧

### 5.1 查看UI状态

```csharp
// 检查UI是否显示
var isShowing = GridFramework.UI.IsShowing<MainMenuUI>();
Debug.Log($"MainMenuUI isShowing: {isShowing}");

// 获取UI状态
var state = GridFramework.UI.GetUIState<MainMenuUI>();
Debug.Log($"MainMenuUI state: {state}");

// 获取UI数量
var count = GridFramework.UI.GetUICount();
Debug.Log($"UI Count: {count}");
```

### 5.2 使用FrameworkLogger

```csharp
// 框架已经有详细的日志输出
// 查看Console中的[UICenter]、[UGUI]、[UILayer]等日志
```

### 5.3 检查组件绑定

```csharp
protected override void OnShow(params object[] args)
{
    base.OnShow(args);
    
    // 检查组件是否正确绑定
    Debug.Assert(_startButton != null, "Start button is null!");
    Debug.Assert(_titleText != null, "Title text is null!");
}
```

---

## 📌 推荐的开发流程

1. **设计阶段**
   - 在纸上或设计软件中规划UI布局
   - 明确哪些组件需要动态访问

2. **制作阶段**
   - 在Unity中制作UI Prefab
   - 给需要访问的组件添加@标记
   - 注意层级结构，保持清晰

3. **生成阶段**
   - 使用生成器自动生成代码
   - 检查生成的Binding.cs，确认组件都正确绑定

4. **开发阶段**
   - 在Logic.cs中编写业务逻辑
   - 实现事件处理
   - 绑定数据到Observer

5. **测试阶段**
   - 运行游戏测试
   - 检查日志
   - 调试问题

6. **优化阶段**
   - 在UIManifest中调整配置
   - 优化缓存策略
   - 优化预加载

---

## 🎯 代码质量检查清单

提交代码前检查：

- [ ] 所有组件都正确标记
- [ ] 没有手动修改Binding.cs
- [ ] 所有生命周期方法都调用了base
- [ ] 没有在构造函数中访问组件
- [ ] 异步方法正确使用async/await
- [ ] OnDestroy中清理了资源
- [ ] 代码注释完整
- [ ] 无编译警告

---

**文档版本**: v2.0  
**维护者**: GGFramework Team

