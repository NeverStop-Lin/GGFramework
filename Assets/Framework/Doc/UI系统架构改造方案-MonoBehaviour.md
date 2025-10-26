# UI系统架构改造方案：UI类改为MonoBehaviour

> **创建时间**: 2025-10-26  
> **状态**: 设计阶段  
> **目标**: 将UI类从普通C#类改造为MonoBehaviour，确保系统稳定过渡

---

## 📋 目录

1. [当前架构分析](#当前架构分析)
2. [改造目标与动机](#改造目标与动机)
3. [关键挑战](#关键挑战)
4. [改造方案设计](#改造方案设计)
5. [实施步骤](#实施步骤)
6. [风险评估与应对](#风险评估与应对)
7. [兼容性策略](#兼容性策略)
8. [测试计划](#测试计划)

---

## 1. 当前架构分析

### 1.1 继承链

```
UIAttachment (普通类)
  └─ BaseUI (抽象类)
      └─ UGUIBaseUI (抽象类)
          └─ MainMenuUI (用户UI类，partial class)
```

### 1.2 核心特性

| 特性 | 当前实现 | 关键依赖 |
|------|---------|---------|
| **实例化方式** | `Container.Instantiate(Type)` | Zenject DiContainer |
| **生命周期** | Pipeline机制（DoCreate/DoShow/DoReady/DoHide/DoDestroy） | AsyncPipeline |
| **GameObject管理** | UGUIBaseUI内部字段（UIObject） | 手动管理 |
| **依赖注入** | `[Inject]` 属性注入 | Zenject |
| **组件绑定** | 自动生成Binding代码（partial class） | FindComponent方法 |
| **Attachment系统** | 7个Attachment扩展UI行为 | UIAttachment基类 |

### 1.3 关键代码路径

```csharp
// UICenter创建UI
var ui = _container.Resolve<PlaceholderFactory<Type, IBaseUI>>().Create(typeof(T));
ui.Initialize();  // 初始化Pipeline和Attachments
await ui.DoCreate(); // 执行Create Pipeline
await ui.DoShow();   // 执行Show Pipeline
await ui.DoReady();  // 执行Ready Pipeline
```

### 1.4 依赖注入配置

```csharp
// FrameworkInstaller.cs
Container.BindFactory<Type, IBaseUI, PlaceholderFactory<Type, IBaseUI>>()
    .FromFactory<UIFactory>();
```

---

## 2. 改造目标与动机

### 2.1 改造目标

✅ **将UI类改为继承MonoBehaviour**，使UI类真正成为Unity组件

### 2.2 改造动机

| 动机 | 说明 | 优先级 |
|------|------|-------|
| **统一生命周期** | 使用Unity标准生命周期（Awake/Start/OnDestroy等） | 🔴 高 |
| **组件化设计** | UI类可以直接挂载到Prefab上，更符合Unity工作流 | 🔴 高 |
| **Inspector支持** | UI类的字段可以在Inspector中直接编辑和调试 | 🟡 中 |
| **Unity事件支持** | 可以使用OnEnable/OnDisable/Update等Unity事件 | 🟡 中 |
| **更好的调试体验** | 在Hierarchy中直接看到UI组件，更容易调试 | 🟢 低 |
| **与Unity生态集成** | 更容易使用第三方MonoBehaviour组件 | 🟢 低 |

---

## 3. 关键挑战

### 3.1 技术挑战

| 挑战 | 影响范围 | 复杂度 | 风险等级 |
|------|---------|-------|---------|
| **1. MonoBehaviour实例化限制** | UIFactory, UICenter | 🔴 高 | 🔴 高 |
| **2. Zenject注入方式变更** | 所有UI类的依赖注入 | 🔴 高 | 🔴 高 |
| **3. 生命周期协调** | BaseUI, UGUIBaseUI | 🟡 中 | 🟡 中 |
| **4. Pipeline机制适配** | 整个Attachment系统 | 🟡 中 | 🟡 中 |
| **5. 现有UI类迁移** | 所有用户UI类 | 🔴 高 | 🟢 低 |
| **6. Prefab结构调整** | 所有UI Prefab | 🟡 中 | 🟢 低 |

### 3.2 挑战详解

#### 3.2.1 MonoBehaviour实例化限制

**问题**：
- MonoBehaviour不能通过`new`或`Activator.CreateInstance`创建
- 必须通过`GameObject.AddComponent<T>()`或从Prefab实例化

**当前代码**：
```csharp
// UIFactory.cs - 当前实现
var ui = (IBaseUI)_container.Instantiate(uiType); // ❌ 对MonoBehaviour无效
```

**影响**：UIFactory、UICenter的创建逻辑需要完全重写

#### 3.2.2 Zenject注入方式变更

**问题**：
- 当前使用`Container.Instantiate(Type)`注入普通类
- MonoBehaviour需要使用`Container.InstantiatePrefab`或`FromComponentInNewPrefab`

**当前代码**：
```csharp
// BaseUI.cs
[Inject]
protected IUI Center; // ✅ 普通类注入有效
```

**MonoBehaviour注入方式**：
```csharp
// 方式1: Prefab上预先绑定
Container.Bind<MainMenuUI>().FromComponentInNewPrefab(prefab).AsTransient();

// 方式2: GameObject注入
var go = new GameObject("MainMenuUI");
var ui = go.AddComponent<MainMenuUI>();
Container.InjectGameObject(go);
```

**影响**：所有UI类的创建和注入逻辑需要调整

#### 3.2.3 生命周期协调

**问题**：需要协调两套生命周期系统

| Unity生命周期 | 当前Pipeline | 说明 |
|--------------|-------------|------|
| `Awake()` | - | MonoBehaviour创建时 |
| `Start()` | - | 首次启用时 |
| `OnEnable()` | `DoShow()` | 激活时 |
| `OnDisable()` | `DoHide()` | 禁用时 |
| `OnDestroy()` | `DoDestroy()` | 销毁时 |
| - | `DoCreate()` | UI初始化 |
| - | `DoReady()` | UI就绪 |

**冲突点**：
- `DoCreate()`应该映射到`Awake()`还是`Start()`？
- `OnEnable/OnDisable`会自动触发，但`DoShow/DoHide`是手动调用的
- 需要防止生命周期重复触发

---

## 4. 改造方案设计

### 4.1 整体策略：渐进式双轨制

采用**双轨制**策略，新旧架构并存，平滑过渡：

```
阶段1: 新架构并行 (1-2周)
  ├─ 创建MonoBehaviour版本的基类（BaseUIBehaviour）
  ├─ 保持旧的BaseUI/UGUIBaseUI不变
  └─ 新UI使用新基类，旧UI继续使用旧基类

阶段2: 逐步迁移 (2-4周)
  ├─ 迁移核心UI（如MainMenuUI）到新基类
  ├─ 验证功能正常
  └─ 逐个迁移其他UI

阶段3: 废弃旧架构 (1周)
  ├─ 所有UI完成迁移
  ├─ 将旧基类标记为[Obsolete]
  └─ 清理旧代码
```

### 4.2 新架构设计

#### 4.2.1 新的继承链

```
MonoBehaviour
  └─ UIAttachmentBehaviour (MonoBehaviour基类)
      └─ BaseUIBehaviour (抽象类，MonoBehaviour)
          └─ UGUIBaseUIBehaviour (抽象类，MonoBehaviour)
              └─ MainMenuUI (用户UI类)
```

#### 4.2.2 核心类设计

##### 方案A：完全重写（推荐）

**优点**：
- 架构清晰，没有历史包袱
- 可以根据MonoBehaviour特性优化设计
- 代码更易理解和维护

**缺点**：
- 工作量较大
- 需要重新测试所有功能

**新基类代码框架**：

```csharp
// BaseUIBehaviour.cs - 新的MonoBehaviour基类
public abstract class BaseUIBehaviour : MonoBehaviour, IBaseUI
{
    #region 依赖注入
    
    [Inject]
    protected IUI Center;
    
    #endregion
    
    #region Pipeline系统
    
    private readonly List<UIAttachmentBehaviour> _attachments = new List<UIAttachmentBehaviour>();
    private readonly Dictionary<UIState, AsyncPipeline> _uiPipelines = new Dictionary<UIState, AsyncPipeline>();
    protected UIState uiState = UIState.None;
    
    #endregion
    
    #region Unity生命周期
    
    protected virtual void Awake()
    {
        // 初始化Pipeline
        InitializePipeline();
    }
    
    protected virtual void OnEnable()
    {
        // 触发Show Pipeline
        _ = DoShowAsync();
    }
    
    protected virtual void OnDisable()
    {
        // 触发Hide Pipeline
        _ = DoHideAsync();
    }
    
    protected virtual void OnDestroy()
    {
        // 触发Destroy Pipeline
        _ = DoDestroyAsync();
    }
    
    #endregion
    
    #region IBaseUI接口实现
    
    public void Initialize()
    {
        // 初始化Attachments
        InitializeAttachments();
        InitializePipeline();
    }
    
    public async Task<object> DoCreate(params object[] args)
    {
        return await ExecuteStatePipelineAsync(UIState.Create, args);
    }
    
    public async Task<object> DoShow(params object[] args)
    {
        gameObject.SetActive(true); // 激活GameObject
        return await ExecuteStatePipelineAsync(UIState.Show, args);
    }
    
    public async Task<object> DoReady(params object[] args)
    {
        return await ExecuteStatePipelineAsync(UIState.Ready, args);
    }
    
    public async Task<object> DoHide(params object[] args)
    {
        await ExecuteStatePipelineAsync(UIState.Hide, args);
        gameObject.SetActive(false); // 禁用GameObject
        return null;
    }
    
    public async Task<object> DoDestroy(params object[] args)
    {
        await ExecuteStatePipelineAsync(UIState.Destroy, args);
        Destroy(gameObject); // 销毁GameObject
        return null;
    }
    
    #endregion
    
    #region 子类重写
    
    protected virtual void OnCreate(params object[] args) { }
    protected virtual void OnShow(params object[] args) { }
    protected virtual void OnReady(params object[] args) { }
    protected virtual void OnHide(params object[] args) { }
    protected virtual void OnDestroy(params object[] args) { }
    
    #endregion
}
```

```csharp
// UGUIBaseUIBehaviour.cs - UGUI实现
public abstract class UGUIBaseUIBehaviour : BaseUIBehaviour
{
    #region 组件
    
    protected Canvas Canvas;
    protected RectTransform RectTransform;
    
    #endregion
    
    #region Unity生命周期
    
    protected override void Awake()
    {
        base.Awake();
        
        // 获取组件
        Canvas = GetComponent<Canvas>();
        RectTransform = GetComponent<RectTransform>();
        
        // 绑定UI组件（自动生成的代码）
        BindComponents();
    }
    
    protected override void OnEnable()
    {
        base.OnEnable();
        RegisterEvents();
    }
    
    protected override void OnDisable()
    {
        UnregisterEvents();
        base.OnDisable();
    }
    
    #endregion
    
    #region 配置
    
    private UIConfig _config;
    
    protected virtual UIConfig CreateUIConfig()
    {
        return new UIConfig
        {
            UIType = UIType.Main,
            AlignType = UIAlignType.Center,
            CacheStrategy = UICacheStrategy.AlwaysCache
        };
    }
    
    #endregion
    
    #region 组件绑定
    
    protected virtual void BindComponents() { }
    protected virtual void RegisterEvents() { }
    protected virtual void UnregisterEvents() { }
    
    protected T FindComponent<T>(string path) where T : Component
    {
        var trans = transform.Find(path);
        if (trans == null)
            throw new Exception($"找不到节点: {path} in {GetType().Name}");
        
        var component = trans.GetComponent<T>();
        if (component == null)
            throw new Exception($"找不到组件: {typeof(T).Name} at {path} in {GetType().Name}");
        
        return component;
    }
    
    #endregion
    
    #region 层级管理
    
    public override int GetIndex()
    {
        return Canvas != null ? Canvas.sortingOrder : 0;
    }
    
    public override void SetIndex(int i)
    {
        if (Canvas != null)
        {
            Canvas.sortingOrder = i;
        }
    }
    
    #endregion
}
```

##### 方案B：适配器模式（保守方案）

**优点**：
- 最小改动，复用现有代码
- 风险较低
- 快速实现

**缺点**：
- 增加一层间接层，性能略有损失
- 代码结构复杂
- 长期维护成本高

**适配器代码框架**：

```csharp
// UIBehaviourAdapter.cs - 适配器
public class UIBehaviourAdapter : MonoBehaviour, IBaseUI
{
    private BaseUI _innerUI; // 内部持有旧的BaseUI
    
    public void Initialize()
    {
        _innerUI.Initialize();
    }
    
    public Task<object> DoCreate(params object[] args) => _innerUI.DoCreate(args);
    public Task<object> DoShow(params object[] args) => _innerUI.DoShow(args);
    public Task<object> DoReady(params object[] args) => _innerUI.DoReady(args);
    public Task<object> DoHide(params object[] args) => _innerUI.DoHide(args);
    public Task<object> DoDestroy(params object[] args) => _innerUI.DoDestroy(args);
    
    void Awake() => _innerUI.DoCreate();
    void OnEnable() => _innerUI.DoShow();
    void OnDisable() => _innerUI.DoHide();
    void OnDestroy() => _innerUI.DoDestroy();
}
```

**结论**：**推荐方案A（完全重写）**，长远来看更利于维护。

#### 4.2.3 UIFactory改造

**新的UIFactory实现**：

```csharp
// UIFactoryBehaviour.cs - MonoBehaviour版本的工厂
public class UIFactoryBehaviour : IFactory<Type, IBaseUI>
{
    private readonly DiContainer _container;
    private readonly Transform _uiRoot;
    
    public UIFactoryBehaviour(DiContainer container)
    {
        _container = container;
        _uiRoot = UIRootManager.GetOrCreateUIRoot();
    }
    
    public IBaseUI Create(Type uiType)
    {
        // 方式1: 从Prefab创建（推荐）
        var config = UIManifestManager.GetConfig(uiType);
        if (!string.IsNullOrEmpty(config?.ResourcePath))
        {
            return CreateFromPrefab(uiType, config.ResourcePath);
        }
        
        // 方式2: 动态创建（兼容模式）
        return CreateDynamic(uiType);
    }
    
    private IBaseUI CreateFromPrefab(Type uiType, string resourcePath)
    {
        // 加载Prefab
        var prefab = Resources.Load<GameObject>(resourcePath);
        if (prefab == null)
        {
            throw new Exception($"无法加载UI Prefab: {resourcePath}");
        }
        
        // 实例化Prefab
        var go = _container.InstantiatePrefab(prefab, _uiRoot);
        
        // 获取或添加UI组件
        var ui = go.GetComponent(uiType) as IBaseUI;
        if (ui == null)
        {
            // Prefab上没有UI组件，动态添加
            ui = go.AddComponent(uiType) as IBaseUI;
            _container.Inject(ui); // 注入依赖
        }
        
        return ui;
    }
    
    private IBaseUI CreateDynamic(Type uiType)
    {
        // 创建新GameObject
        var go = new GameObject(uiType.Name);
        go.transform.SetParent(_uiRoot, false);
        
        // 添加UI组件
        var ui = go.AddComponent(uiType) as IBaseUI;
        
        // Zenject注入
        _container.InjectGameObject(go);
        
        return ui;
    }
}
```

#### 4.2.4 UICenter改造

**关键变更**：

```csharp
// UICenter.cs - 改造后
public class UICenter : IUI
{
    public UiLifeCycle<T> Show<T>(params object[] args) where T : IBaseUI
    {
        // ⚠️ 注意：不再使用 new() 约束
        var uiType = typeof(T);
        
        if (uiState.Ui == null)
        {
            // 通过工厂创建（MonoBehaviour）
            var ui = _container.Resolve<PlaceholderFactory<Type, IBaseUI>>().Create(uiType);
            ui.Initialize();
            uiState.Ui = ui;
            
            // ... 后续逻辑
        }
        
        // ...
    }
}
```

**接口变更**：

```csharp
// IUI.cs - 移除 new() 约束
public interface IUI
{
    // ❌ 旧版本
    // UiLifeCycle<T> Show<T>(params object[] args) where T : IBaseUI, new();
    
    // ✅ 新版本
    UiLifeCycle<T> Show<T>(params object[] args) where T : IBaseUI;
    
    // ✅ 或者使用更明确的约束
    UiLifeCycle<T> Show<T>(params object[] args) where T : MonoBehaviour, IBaseUI;
}
```

#### 4.2.5 Prefab结构调整

**当前Prefab结构**（UI组件不在Prefab上）：

```
MainMenuUI.prefab
  └─ Panel
      ├─ @Button_Start (Button)
      ├─ @Button_Settings (Button)
      └─ @Text_Title (Text)
```

**新Prefab结构**（UI组件挂载在Prefab根节点）：

```
MainMenuUI.prefab [附加 MainMenuUI.cs 组件]
  ├─ Canvas
  ├─ RectTransform
  └─ Panel
      ├─ @Button_Start (Button)
      ├─ @Button_Settings (Button)
      └─ @Text_Title (Text)
```

**自动迁移工具**：

```csharp
// Editor/UIPrefabMigrationTool.cs
public class UIPrefabMigrationTool : EditorWindow
{
    public void MigratePrefab(GameObject prefab, Type uiType)
    {
        // 1. 添加UI组件到根节点
        var uiComponent = prefab.AddComponent(uiType);
        
        // 2. 确保有Canvas和RectTransform
        if (prefab.GetComponent<Canvas>() == null)
        {
            prefab.AddComponent<Canvas>();
        }
        
        // 3. 保存Prefab
        PrefabUtility.SavePrefabAsset(prefab);
        
        Debug.Log($"✅ Prefab迁移完成: {prefab.name}");
    }
}
```

---

## 5. 实施步骤

### 5.1 阶段0：准备工作（1-2天）

#### ✅ 任务清单

- [ ] **代码备份**
  - [ ] 创建Git分支 `feature/ui-monobehaviour`
  - [ ] 提交当前所有改动
  - [ ] 备份关键文件

- [ ] **依赖梳理**
  - [ ] 列出所有继承自UGUIBaseUI的UI类
  - [ ] 列出所有依赖UI系统的模块
  - [ ] 绘制依赖关系图

- [ ] **测试基准**
  - [ ] 记录当前UI系统的所有功能点
  - [ ] 运行现有测试，记录结果
  - [ ] 建立性能基准（UI创建时间、内存占用等）

### 5.2 阶段1：新架构并行（1周）

#### 任务1.1：创建新基类

```
Assets/Framework/Core/Systems/UI/Core/
  ├─ BaseUIBehaviour.cs        (新建)
  └─ UGUIBaseUIBehaviour.cs    (新建)
```

- [ ] 创建`BaseUIBehaviour.cs`
  - [ ] 实现IBaseUI接口
  - [ ] 实现Pipeline机制
  - [ ] 实现Attachment系统
  - [ ] 协调Unity生命周期
  
- [ ] 创建`UGUIBaseUIBehaviour.cs`
  - [ ] 继承BaseUIBehaviour
  - [ ] 实现组件查找
  - [ ] 实现层级管理
  - [ ] 实现配置系统

#### 任务1.2：改造工厂和管理器

- [ ] 创建`UIFactoryBehaviour.cs`
  - [ ] 实现从Prefab创建UI
  - [ ] 实现动态创建UI
  - [ ] 支持Zenject注入

- [ ] 修改`FrameworkInstaller.cs`
  - [ ] 绑定新工厂
  - [ ] 保持旧工厂绑定（双轨）

```csharp
// FrameworkInstaller.cs - 双轨绑定
// 旧版本工厂（保留）
Container.BindFactory<Type, IBaseUI, PlaceholderFactory<Type, IBaseUI>>()
    .FromFactory<UIFactory>()
    .WithId("Legacy");

// 新版本工厂
Container.BindFactory<Type, IBaseUI, PlaceholderFactory<Type, IBaseUI>>()
    .FromFactory<UIFactoryBehaviour>()
    .WithId("MonoBehaviour");

// UICenter可以根据UI类型选择工厂
```

#### 任务1.3：修改UICenter

- [ ] 添加工厂选择逻辑
  - [ ] 检测UI类型（是否继承MonoBehaviour）
  - [ ] 选择对应的工厂

```csharp
// UICenter.cs - 智能工厂选择
private IBaseUI CreateUI(Type uiType)
{
    if (typeof(MonoBehaviour).IsAssignableFrom(uiType))
    {
        // 使用新工厂
        return _container.ResolveId<PlaceholderFactory<Type, IBaseUI>>("MonoBehaviour").Create(uiType);
    }
    else
    {
        // 使用旧工厂
        return _container.ResolveId<PlaceholderFactory<Type, IBaseUI>>("Legacy").Create(uiType);
    }
}
```

### 5.3 阶段2：迁移测试UI（3-5天）

#### 任务2.1：迁移第一个测试UI

选择一个简单的UI作为测试（如TestUI）：

- [ ] **调整Prefab**
  - [ ] 在Prefab根节点添加TestUI组件
  - [ ] 添加Canvas和RectTransform组件
  
- [ ] **修改UI类**
  - [ ] 从`UGUIBaseUI`改为`UGUIBaseUIBehaviour`
  - [ ] 调整生命周期方法（如果有冲突）
  - [ ] 更新Binding代码生成器（如果需要）

```csharp
// TestUI.cs - 迁移前
public partial class TestUI : UGUIBaseUI
{
    // ...
}

// TestUI.cs - 迁移后
public partial class TestUI : UGUIBaseUIBehaviour
{
    // ...
}
```

- [ ] **功能验证**
  - [ ] 显示/隐藏
  - [ ] 事件响应
  - [ ] 生命周期回调
  - [ ] 依赖注入
  - [ ] Attachment功能

#### 任务2.2：迁移核心UI

- [ ] MainMenuUI
- [ ] GameUI
- [ ] (其他常用UI)

每个UI迁移后都要进行完整测试。

### 5.4 阶段3：全量迁移（1-2周）

- [ ] **批量迁移**
  - [ ] 使用迁移工具批量处理Prefab
  - [ ] 批量替换基类引用
  - [ ] 更新所有Binding代码

- [ ] **完整回归测试**
  - [ ] 所有UI功能测试
  - [ ] 性能测试
  - [ ] 内存泄漏检查

### 5.5 阶段4：清理旧代码（3-5天）

- [ ] **标记废弃**
  - [ ] 给`BaseUI`和`UGUIBaseUI`添加`[Obsolete]`特性
  - [ ] 给`UIFactory`添加`[Obsolete]`特性

```csharp
[Obsolete("请使用 BaseUIBehaviour 代替", true)]
public abstract class BaseUI : UIAttachment, IBaseUI
{
    // ...
}
```

- [ ] **移除双轨逻辑**
  - [ ] 删除工厂选择逻辑
  - [ ] 只保留MonoBehaviour工厂

- [ ] **移动到Deprecated目录**
  - [ ] 将旧基类移动到`Systems/UI/Deprecated/`
  - [ ] 更新目录结构说明文档

---

## 6. 风险评估与应对

### 6.1 风险矩阵

| 风险 | 概率 | 影响 | 等级 | 应对策略 |
|------|------|------|------|---------|
| **Zenject注入失败** | 🟡 中 | 🔴 高 | 🔴 高 | 1. 详细测试各种注入场景<br>2. 准备回退方案 |
| **生命周期冲突** | 🟡 中 | 🟡 中 | 🟡 中 | 1. 使用标志位防止重复调用<br>2. 明确文档说明 |
| **性能下降** | 🟢 低 | 🟡 中 | 🟢 低 | 1. 性能基准测试<br>2. 优化热点代码 |
| **现有UI功能失效** | 🟡 中 | 🔴 高 | 🔴 高 | 1. 完整回归测试<br>2. 逐个迁移而非一次性 |
| **第三方插件不兼容** | 🟢 低 | 🟡 中 | 🟢 低 | 1. 提前测试关键插件<br>2. 准备适配器 |
| **Attachment系统失效** | 🟡 中 | 🔴 高 | 🔴 高 | 1. 重点测试Attachment<br>2. 保留旧Attachment实现 |

### 6.2 应对措施

#### 6.2.1 技术回退方案

**回退触发条件**：
- 关键功能失效且无法在1天内修复
- 出现严重性能问题
- 导致其他系统崩溃

**回退步骤**：
1. 切换回旧的Git分支
2. 恢复旧的FrameworkInstaller配置
3. 所有新UI类改回继承旧基类
4. 运行完整测试

#### 6.2.2 分阶段验证

每完成一个阶段，必须通过以下验证：

✅ **功能验证**
- [ ] 所有UI能正常显示/隐藏
- [ ] 所有按钮事件正常响应
- [ ] UI栈功能正常
- [ ] 预加载功能正常

✅ **性能验证**
- [ ] UI创建时间不超过旧版本的120%
- [ ] 内存占用不超过旧版本的110%
- [ ] 无内存泄漏

✅ **兼容性验证**
- [ ] 所有依赖UI系统的模块正常工作
- [ ] Zenject注入正常
- [ ] Attachment系统正常

---

## 7. 兼容性策略

### 7.1 双轨并行期（阶段1-2）

```
旧架构                新架构
  ↓                     ↓
BaseUI           BaseUIBehaviour
  ↓                     ↓
UGUIBaseUI       UGUIBaseUIBehaviour
  ↓                     ↓
[旧UI类]          [新UI类]
```

**关键点**：
1. 两套基类完全独立，互不影响
2. UICenter智能选择工厂
3. IBaseUI接口作为统一抽象

### 7.2 API兼容性

**保持不变的API**：
- `GridFramework.UI.Show<T>()`
- `GridFramework.UI.Hide<T>()`
- `GridFramework.UI.GetUI<T>()`
- 所有IUI接口方法

**需要调整的代码**：
- UI类的基类继承（`UGUIBaseUI` → `UGUIBaseUIBehaviour`）
- Prefab结构（添加UI组件）
- 部分生命周期方法的调用时机

**不需要修改的代码**：
- UI类的业务逻辑代码
- 外部调用UI的代码
- Binding生成的代码结构

---

## 8. 测试计划

### 8.1 单元测试

```csharp
// Tests/Editor/UISystemTests.cs
[TestFixture]
public class UIMonoBehaviourTests
{
    [Test]
    public void TestUICreation()
    {
        // 测试UI创建
        var ui = UICenter.Show<TestUI>();
        Assert.NotNull(ui.Target);
        Assert.IsInstanceOf<MonoBehaviour>(ui.Target);
    }
    
    [Test]
    public void TestZenjectInjection()
    {
        // 测试依赖注入
        var ui = UICenter.Show<TestUI>();
        Assert.NotNull(ui.Target.Center);
    }
    
    [Test]
    public void TestLifecycle()
    {
        // 测试生命周期
        var ui = UICenter.Show<TestUI>();
        Assert.AreEqual(UIRuntimeState.Showing, UICenter.GetUIState<TestUI>());
        
        UICenter.Hide<TestUI>();
        Assert.AreEqual(UIRuntimeState.Hidden, UICenter.GetUIState<TestUI>());
    }
    
    [Test]
    public void TestAttachment()
    {
        // 测试Attachment系统
        var ui = UICenter.Show<TestUI>();
        // 验证Attachment回调被触发
    }
}
```

### 8.2 集成测试

- [ ] **UI显示测试**：测试所有UI的显示和隐藏
- [ ] **UI栈测试**：测试PushUI和PopUI功能
- [ ] **事件测试**：测试所有按钮点击事件
- [ ] **动画测试**：测试UI动画（如果有）
- [ ] **多场景测试**：测试在不同场景间切换

### 8.3 性能测试

```csharp
// Tests/Performance/UIPerformanceTests.cs
[Test, Performance]
public void TestUICreationPerformance()
{
    Measure.Method(() =>
    {
        GridFramework.UI.Show<TestUI>();
    })
    .WarmupCount(10)
    .MeasurementCount(100)
    .Run();
}

[Test, Performance]
public void TestUIMemoryUsage()
{
    // 测试内存占用
    var before = Profiler.GetTotalAllocatedMemoryLong();
    
    for (int i = 0; i < 100; i++)
    {
        GridFramework.UI.Show<TestUI>();
        GridFramework.UI.Hide<TestUI>();
    }
    
    var after = Profiler.GetTotalAllocatedMemoryLong();
    var diff = after - before;
    
    Assert.Less(diff, 10 * 1024 * 1024); // 不超过10MB
}
```

### 8.4 手动测试清单

- [ ] **UI显示**
  - [ ] 各种UI类型（Main/Popup/Effect）显示正常
  - [ ] UI层级正确
  - [ ] UI对齐正确

- [ ] **交互**
  - [ ] 按钮点击响应正常
  - [ ] 输入框输入正常
  - [ ] 滚动列表滚动正常

- [ ] **生命周期**
  - [ ] OnShow/OnHide回调正确触发
  - [ ] OnCreate/OnDestroy回调正确触发
  - [ ] OnReady回调正确触发

- [ ] **高级功能**
  - [ ] UI预加载功能正常
  - [ ] UI缓存策略生效
  - [ ] UI栈功能正常
  - [ ] EventBus事件正常触发

---

## 9. 总结

### 9.1 方案优势

✅ **渐进式迁移**：双轨并行，风险可控  
✅ **兼容性好**：外部API不变，业务代码不受影响  
✅ **可回退**：任何阶段都可以回退到旧版本  
✅ **清晰架构**：新架构符合Unity标准实践  

### 9.2 关键成功因素

1. **充分测试**：每个阶段都要完整测试
2. **逐步迁移**：不要一次性迁移所有UI
3. **文档同步**：及时更新文档和示例
4. **团队沟通**：确保所有开发者了解变更

### 9.3 预估工作量

| 阶段 | 工作量 | 时间 |
|------|--------|------|
| 阶段0：准备 | 1人日 | 1-2天 |
| 阶段1：新架构 | 3人日 | 3-5天 |
| 阶段2：测试迁移 | 2人日 | 2-3天 |
| 阶段3：全量迁移 | 5人日 | 1-2周 |
| 阶段4：清理 | 2人日 | 2-3天 |
| **总计** | **13人日** | **3-4周** |

### 9.4 下一步行动

1. ✅ **方案评审**：团队评审本方案，确认可行性
2. ⏳ **创建分支**：`feature/ui-monobehaviour`
3. ⏳ **开始阶段0**：准备工作和基准测试
4. ⏳ **开始阶段1**：创建新基类

---

## 附录

### A. 关键代码文件清单

| 文件 | 路径 | 改动类型 |
|------|------|---------|
| BaseUIBehaviour.cs | Assets/Framework/Core/Systems/UI/Core/ | 🆕 新建 |
| UGUIBaseUIBehaviour.cs | Assets/Framework/Core/Systems/UI/Core/ | 🆕 新建 |
| UIFactoryBehaviour.cs | Assets/Framework/Core/Systems/UI/Core/ | 🆕 新建 |
| UICenter.cs | Assets/Framework/Core/Systems/UI/Managers/ | 🔄 修改 |
| FrameworkInstaller.cs | Assets/Framework/Scripts/ | 🔄 修改 |
| IUI.cs | Assets/Framework/Core/Interface/ | 🔄 修改（移除new()约束） |
| BaseUI.cs | Assets/Framework/Core/Systems/UI/Core/ | 🔶 废弃 |
| UGUIBaseUI.cs | Assets/Framework/Core/Systems/UI/Core/ | 🔶 废弃 |
| UIFactory.cs | Assets/Framework/Core/Systems/UI/Core/ | 🔶 废弃 |

### B. 参考资料

- [Zenject MonoBehaviour Injection](https://github.com/modesttree/Zenject#game-object-bind-methods)
- [Unity生命周期文档](https://docs.unity3d.com/Manual/ExecutionOrder.html)
- [MonoBehaviour最佳实践](https://unity.com/how-to/use-monobehaviours-your-code)

---

**文档维护**：本文档将在实施过程中持续更新。

**最后更新**：2025-10-26

