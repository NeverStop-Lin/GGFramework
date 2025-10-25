# GGFramework - Unity 游戏开发框架

基于 UGUI 的模块化 Unity 游戏开发框架，采用清晰的分层架构设计。

## 📁 目录结构

```
Framework/
├── Core/              [核心框架层]
│   ├── Systems/      核心系统（UI、Observer、Config等）
│   ├── Interface/    接口定义
│   ├── Utils/        工具类
│   └── Common/       公共代码
│
├── Modules/          [通用模块层]
│   ├── Sound/        音效系统
│   ├── Analytics/    数据分析
│   ├── Advertise/    广告集成
│   ├── User/         用户管理
│   └── UI/           通用UI组件
│
├── Scripts/          [框架入口]
│   ├── GridFramework.cs    核心系统入口
│   ├── GridModules.cs      模块系统入口
│   └── Installers/         DI配置
│
├── Editor/           [编辑器工具]
│   ├── Excel/        配置表生成
│   └── FontOptimizer/ 字体优化
│
└── Resources/        [统一资源]
    ├── CoreInstaller.asset
    └── ModulesInstaller.asset
```

## 🚀 快速开始

### 访问核心系统

```csharp
using Framework.Scripts;

// UI 系统
GridFramework.UI.Show<MyUI>();
GridFramework.UI.Hide<MyUI>();

// Observer 系统
var goldObserver = GridFramework.Observer.Value(100);
goldObserver.OnChange.Add((newVal, oldVal) => {
    Debug.Log($"金币变化: {oldVal} -> {newVal}");
});

// Config 系统
var config = GridFramework.Config.Get<MyConfig>();

// Timer 系统
GridFramework.Timer.AddTimer("countdown", 1f, () => {
    Debug.Log("定时器触发");
});

// Storage 系统
GridFramework.Storage.Save("key", value);
var data = GridFramework.Storage.Load<int>("key");
```

### 使用通用模块

```csharp
using Framework.Scripts;

// 音效
GridModules.Sound.PlayMusic("bgm_main");
GridModules.Sound.PlayEffect("click");

// 数据上报
GridModules.Analytics.OnLevelEnter();
GridModules.Analytics.OnItemLevelUp(1, 100, 5);

// 广告
bool success = await GridModules.Advertise.Video.Show("double_reward");

// 通用UI
GridModules.CommonUI.ShowToast("提示信息");
```

## ✨ 核心特性

### Core 层（核心框架）

- **6 大核心系统**：UI、Observer、Config、Timer、Storage、Event
- **完整的接口设计**：IUI、IObservers、IConfigs、ITimer、IStorage
- **丰富的工具类**：Pipeline、Extensions、BaseType 等
- **依赖注入支持**：基于 Zenject 的完整 DI 框架

### Modules 层（通用模块）

- **Sound**：音乐和音效管理
- **Analytics**：数据分析和上报
- **Advertise**：广告集成（视频广告）
- **User**：用户管理（可扩展）
- **UI**：通用UI组件（Toast、Loading等）

## 🏗️ 架构设计

### 分层架构

```
┌─────────────────────────────────────┐
│         游戏业务代码（项目层）         │ 
└──────────────┬──────────────────────┘
               │ 使用
               ↓
┌─────────────────────────────────────┐
│      Modules（通用模块层）            │
│  Sound / Analytics / Advertise ...  │
└──────────────┬──────────────────────┘
               │ 依赖
               ↓
┌─────────────────────────────────────┐
│       Core（核心框架层）              │
│  Systems / Interface / Utils ...    │
└─────────────────────────────────────┘
```

### 核心入口

```
Scripts/                    [统一入口点]
├── GridFramework.cs       访问核心系统
└── GridModules.cs         访问通用模块
```

## 📦 依赖

- Unity 2021.3+
- Zenject (依赖注入)
- Unity UGUI (内置)

## 📚 文档

- [Core 层文档](Core/README.md)
- [Modules 层文档](Modules/README.md)
- [开发扩展编码规范](开发扩展编码规范.md)

## 🎯 适用场景

- **小项目**：仅使用 Core 层
- **中型项目**：Core + 部分 Modules
- **大型项目**：Core + Modules + 自定义扩展

## 🔄 版本历史

### v2.0.0 (当前)
- ✅ 重组为 Core/Modules 架构
- ✅ 移除 FairyGUI 依赖
- ✅ 迁移到 UGUI
- ✅ 完善模块化设计
- ✅ 优化命名空间（缩短 38%）

### v1.0.0
- 基于 FairyGUI 的原始版本

## 📄 许可证

MIT License
