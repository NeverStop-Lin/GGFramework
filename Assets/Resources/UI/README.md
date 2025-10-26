# UI预制体目录

此目录用于存放游戏UI的Prefab文件。

## 📁 目录结构

```
UI/
├─ Main/                    主界面UI Prefab
│  ├─ MainMenu.prefab
│  └─ GamePlay.prefab
│
├─ Popup/                   弹窗UI Prefab
│  ├─ Reward.prefab
│  ├─ Confirm.prefab
│  └─ Settings.prefab
│
└─ Common/                  通用UI组件 Prefab
   ├─ Loading.prefab
   ├─ Toast.prefab
   └─ Tips.prefab
```

## 📝 命名规范

### Prefab命名
- 使用帕斯卡命名法（PascalCase）
- 清晰描述UI功能
- 不需要UI后缀

**正确示例**：
```
MainMenu.prefab           ✅
SettingsPanel.prefab      ✅
RewardPopup.prefab        ✅
```

**错误示例**：
```
main_menu.prefab          ❌ 使用了下划线
MainMenuUI.prefab         ❌ 不需要UI后缀
```

## 🏷️ 组件标记规范

在Prefab的Hierarchy中，给需要在代码中访问的组件添加`@`前缀：

```
MainMenu.prefab
  └─ Canvas
      └─ Panel
          ├─ @Button_Start       ← Button组件，会在代码中生成 _startButton
          ├─ @Button_Settings    ← Button组件，会在代码中生成 _settingsButton  
          ├─ @Text_Title         ← Text组件，会在代码中生成 _titleText
          └─ @Image_Logo         ← Image组件，会在代码中生成 _logoImage
```

### 支持的标记

| 标记格式 | 组件类型 |
|---------|---------|
| `@Button_xxx` | Button |
| `@Text_xxx` | Text |
| `@TextTMP_xxx` | TextMeshProUGUI |
| `@Image_xxx` | Image |
| `@Input_xxx` | InputField |
| `@Toggle_xxx` | Toggle |
| `@Slider_xxx` | Slider |
| `@Transform_xxx` | Transform |
| `@GameObject_xxx` | GameObject |

## 🚀 如何使用

1. 在此目录创建UI Prefab
2. 标记需要绑定的组件
3. 使用工具生成代码：`Tools -> UI工具 -> 生成UI代码`
4. 编写业务逻辑
5. 在游戏中调用 `GridFramework.UI.Show<XXX>()`

## 📖 详细文档

- [UI系统使用指南](../Framework/Doc/UI系统使用指南.md)
- [UI命名规范](../Framework/Doc/UI命名规范.md)
- [UI最佳实践](../Framework/Doc/UI最佳实践.md)

