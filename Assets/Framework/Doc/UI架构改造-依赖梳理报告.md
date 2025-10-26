# UI架构改造 - 依赖梳理报告

> **创建时间**: 2025-10-26  
> **分支**: feature/ui-monobehaviour  
> **目的**: 记录UI系统的所有依赖关系，为架构改造做准备

---

## 1. 继承UGUIBaseUI的UI类清单

### 1.1 用户UI类（2个）

| UI类 | 路径 | 类型 | 状态 | 优先级 |
|------|------|------|------|-------|
| **MainMenuUI** | Assets/Game/Scripts/UI/MainMenuUI.cs | Main UI | ✅ 活跃 | 🔴 高 |
| **GameUI** | Assets/Game/Scripts/UI/GameUI.cs | Main UI | ✅ 活跃 | 🔴 高 |

**总计**: 2个用户UI类

### 1.2 废弃基类（2个）

| 类 | 路径 | 状态 | 说明 |
|------|------|------|------|
| **MainUI** | Assets/Framework/Core/Systems/UI/Deprecated/MainUI.cs | 🔶 已废弃 | 标记[Obsolete] |
| **PopupUI** | Assets/Framework/Core/Systems/UI/Deprecated/PopupUI.cs | 🔶 已废弃 | 标记[Obsolete] |

**说明**: 这两个类已经是废弃类，不需要迁移。

---

## 2. 依赖UI系统的模块

### 2.1 直接依赖（通过GridFramework.UI调用）

#### 2.1.1 Launcher（启动器）

**文件**: `Assets/Game/Launcher.cs`

```csharp
void Start()
{
    GridFramework.UI.Show<MainMenuUI>();  // 显示主菜单
}
```

**影响**: 无需修改（IUI接口不变）

#### 2.1.2 MainMenuUI（UI间跳转）

**文件**: `Assets/Game/Scripts/UI/MainMenuUI.cs`

```csharp
private void OnStartClick()
{
    GridFramework.UI.Show<GameUI>();  // 显示游戏UI
    Hide();  // 隐藏自己
}
```

**影响**: 无需修改（IUI接口不变）

### 2.2 核心框架依赖

| 模块 | 依赖方式 | 影响 |
|------|---------|------|
| **GridFramework** | 静态访问 `GridFramework.UI` | ✅ 无影响 |
| **FrameworkInstaller** | 绑定IUI接口 | 🟡 需要修改（添加双轨绑定） |
| **UICenter** | 管理UI生命周期 | 🟡 需要修改（添加工厂选择） |
| **UIFactory** | 创建UI实例 | 🔴 需要新建（MonoBehaviour版本） |

### 2.3 间接依赖

| 系统 | 依赖关系 | 影响 |
|------|---------|------|
| **Zenject** | 依赖注入 | ✅ 无影响（继续使用） |
| **资源系统** | 加载UI Prefab | ✅ 无影响 |
| **事件系统** | UI生命周期事件 | ✅ 无影响 |

---

## 3. 核心文件清单

### 3.1 需要修改的文件（5个）

| 文件 | 路径 | 改动类型 | 复杂度 |
|------|------|---------|-------|
| ✏️ **IUI.cs** | Assets/Framework/Core/Interface/IUI.cs | 移除new()约束 | 🟢 简单 |
| ✏️ **FrameworkInstaller.cs** | Assets/Framework/Scripts/Installers/FrameworkInstaller.cs | 添加双轨绑定 | 🟡 中等 |
| ✏️ **UICenter.cs** | Assets/Framework/Core/Systems/UI/Managers/UICenter.cs | 添加工厂选择逻辑 | 🟡 中等 |
| ✏️ **MainMenuUI.cs** | Assets/Game/Scripts/UI/MainMenuUI.cs | 更改基类继承 | 🟢 简单 |
| ✏️ **GameUI.cs** | Assets/Game/Scripts/UI/GameUI.cs | 更改基类继承 | 🟢 简单 |

### 3.2 需要新建的文件（4个）

| 文件 | 路径 | 说明 |
|------|------|------|
| 🆕 **BaseUIBehaviour.cs** | Assets/Framework/Core/Systems/UI/Core/ | MonoBehaviour版本的BaseUI |
| 🆕 **UGUIBaseUIBehaviour.cs** | Assets/Framework/Core/Systems/UI/Core/ | MonoBehaviour版本的UGUIBaseUI |
| 🆕 **UIFactoryBehaviour.cs** | Assets/Framework/Core/Systems/UI/Core/ | MonoBehaviour工厂 |
| 🆕 **UIAttachmentBehaviour.cs** | Assets/Framework/Core/Systems/UI/Attachment/ | MonoBehaviour版本的Attachment基类 |

### 3.3 保持不变的文件（关键）

| 文件 | 说明 | 原因 |
|------|------|------|
| ✅ **IUI.cs** | UI管理接口 | 只需移除new()约束，方法签名不变 |
| ✅ **IBaseUI.cs** | UI基类接口 | 完全不变 |
| ✅ **UIConfig.cs** | UI配置类 | 完全不变 |
| ✅ **所有Attachment** | Attachment系统 | 暂时保持不变（阶段1） |
| ✅ **所有Manager** | 管理器系统 | 完全不变 |

---

## 4. Prefab资源清单

### 4.1 需要调整的Prefab（2个）

| Prefab | 路径 | 当前结构 | 需要调整 |
|--------|------|---------|---------|
| **MainMenuUI** | Assets/Game/Resources/UI/MainMenuUI.prefab | 根节点无UI组件 | ✅ 需要添加MainMenuUI组件 |
| **GameUI** | Assets/Game/Resources/UI/GameUI.prefab | 根节点无UI组件 | ✅ 需要添加GameUI组件 |

**调整方式**: 
1. 在Prefab根节点添加对应的UI组件
2. 确保有Canvas和RectTransform组件
3. 保存Prefab

---

## 5. 测试点清单

### 5.1 功能测试点

| 功能 | 测试方法 | 预期结果 |
|------|---------|---------|
| **UI显示** | `GridFramework.UI.Show<MainMenuUI>()` | MainMenuUI正常显示 |
| **UI隐藏** | `GridFramework.UI.Hide<MainMenuUI>()` | MainMenuUI正常隐藏 |
| **UI跳转** | MainMenuUI → GameUI | 跳转正常 |
| **按钮事件** | 点击Start按钮 | 响应正常 |
| **生命周期** | OnCreate/OnShow/OnHide回调 | 回调正常触发 |
| **Zenject注入** | UI类中的[Inject]字段 | 注入正常 |

### 5.2 性能测试点

| 指标 | 当前基准 | 目标 | 测量方法 |
|------|---------|------|---------|
| **UI创建时间** | ~15ms | ≤18ms (120%) | Profiler测量 |
| **内存占用** | ~18KB/UI | ≤20KB (110%) | Profiler测量 |
| **GC分配** | ~1KB/次 | ≤1.5KB (150%) | Profiler测量 |

---

## 6. 风险评估

### 6.1 高风险点

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|---------|
| **Zenject注入失败** | 🟡 中 | 🔴 高 | 充分测试，准备回退方案 |
| **生命周期冲突** | 🟡 中 | 🟡 中 | 使用标志位防止重复触发 |

### 6.2 中风险点

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|---------|
| **Prefab结构调整错误** | 🟢 低 | 🟡 中 | 逐个调整，每次测试 |
| **Attachment系统失效** | 🟢 低 | 🟡 中 | 先保持旧Attachment不变 |

---

## 7. 迁移优先级

### 阶段1：新架构并行（1周）
- ✅ 创建新基类
- ✅ 改造工厂和安装器
- ✅ 测试基础功能

### 阶段2：迁移测试（3天）
1. 🔴 **MainMenuUI** - 优先迁移（启动时使用，最关键）
2. 🔴 **GameUI** - 次优先迁移（主要游戏界面）

### 阶段3：清理（可选）
- 标记旧基类为[Obsolete]
- 移除双轨逻辑

---

## 8. 回退策略

### 快速回退步骤

1. 切换回main分支
   ```bash
   git checkout main
   ```

2. 恢复Prefab（如果已修改）
   ```bash
   git checkout HEAD -- Assets/Game/Resources/UI/*.prefab
   ```

3. 重启Unity Editor

**预计回退时间**: < 5分钟

---

## 9. 下一步行动

### ✅ 已完成

- [x] 列出所有UI类（2个）
- [x] 列出依赖模块
- [x] 评估风险
- [x] 制定迁移计划

### ⏳ 待完成

- [ ] 记录性能基准
- [ ] 创建新基类
- [ ] 改造工厂系统
- [ ] 迁移第一个UI

---

## 10. 总结

### 影响范围

- **用户UI类**: 2个（需要迁移）
- **核心文件**: 9个（5个修改 + 4个新建）
- **Prefab**: 2个（需要调整）
- **外部调用**: 无需修改（IUI接口不变）

### 工作量评估

- **阶段1（新架构）**: 3-5天
- **阶段2（迁移测试）**: 2-3天
- **阶段3（全量迁移）**: 1-2天（只有2个UI）
- **总计**: 1-2周

### 风险评级

**整体风险**: 🟢 低

**原因**:
1. UI类数量少（只有2个）
2. 依赖关系清晰
3. 有完整回退方案
4. 采用双轨并行策略

---

**文档状态**: ✅ 完成  
**下一步**: 记录性能基准，开始阶段1

