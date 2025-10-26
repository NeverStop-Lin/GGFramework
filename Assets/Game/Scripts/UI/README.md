# UI脚本目录

此目录用于存放游戏UI的脚本文件。

## 📁 目录结构

```
UI/
├─ Main/                    主界面UI
│  ├─ MainMenuUI.cs
│  ├─ MainMenuUI.Binding.cs
│  ├─ GamePlayUI.cs
│  └─ GamePlayUI.Binding.cs
│
├─ Popup/                   弹窗UI
│  ├─ RewardUI.cs
│  ├─ RewardUI.Binding.cs
│  ├─ ConfirmUI.cs
│  └─ ConfirmUI.Binding.cs
│
└─ Common/                  通用UI组件
   ├─ LoadingUI.cs
   ├─ LoadingUI.Binding.cs
   ├─ ToastUI.cs
   └─ ToastUI.Binding.cs
```

## 📝 文件说明

### XXX.cs（业务逻辑文件）
- **可以手动修改**
- 包含UI配置和业务逻辑
- 首次生成后不会被覆盖

### XXX.Binding.cs（组件绑定文件）
- **自动生成，请勿修改**
- 包含组件字段声明和绑定代码
- 每次重新生成都会被覆盖

## 🚀 如何创建新UI

1. 在Unity中设计UI Prefab，保存到 `Assets/Resources/UI/`
2. 给需要绑定的组件添加@标记（如 `@Button_Start`）
3. 使用工具生成代码：`Tools -> UI工具 -> 生成UI代码`
4. 在生成的Logic文件中编写业务逻辑

详见：[UI系统使用指南](../../Framework/Doc/UI系统使用指南.md)

## 📖 相关文档

- [UI系统使用指南](../../Framework/Doc/UI系统使用指南.md)
- [UI命名规范](../../Framework/Doc/UI命名规范.md)
- [UI最佳实践](../../Framework/Doc/UI最佳实践.md)

