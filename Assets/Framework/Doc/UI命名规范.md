# UI命名规范

> **版本**: v2.0  
> **更新日期**: 2025-01-26

---

## 📋 目录

1. [Prefab命名](#1-prefab命名)
2. [UI类命名](#2-ui类命名)
3. [组件标记命名](#3-组件标记命名)
4. [文件命名](#4-文件命名)
5. [目录结构](#5-目录结构)

---

## 1. Prefab命名

### 规则
- 使用帕斯卡命名法（PascalCase）
- 清晰描述UI功能
- 不需要UI后缀（代码生成时自动添加）

### ✅ 正确示例
```
MainMenu.prefab          主菜单
SettingsPanel.prefab     设置面板
RewardPopup.prefab       奖励弹窗
PlayerInfo.prefab        玩家信息
ShopItem.prefab          商店物品
```

### ❌ 错误示例
```
main_menu.prefab         ❌ 使用了下划线
mainmenu.prefab          ❌ 没有驼峰
MainMenuUI.prefab        ❌ 不需要UI后缀
ui_main.prefab           ❌ UI不应该是前缀
```

---

## 2. UI类命名

### 规则
- 自动生成，基于Prefab名称
- 自动添加UI后缀
- 与Prefab名称对应

### 对应关系
```
MainMenu.prefab       → MainMenuUI.cs
SettingsPanel.prefab  → SettingsPanelUI.cs
RewardPopup.prefab    → RewardPopupUI.cs
```

### 文件结构
```
MainMenuUI.cs          业务逻辑（手写）
MainMenuUI.Binding.cs  组件绑定（自动生成）
```

---

## 3. 组件标记命名

### 3.1 基本规则

**格式**：`@组件类型_组件名称`

**组件名称规则**：
- 使用帕斯卡命名法
- 多个单词用下划线分隔
- 清晰描述组件用途

### 3.2 标记示例

#### Button按钮
```
@Button_Start           开始按钮
@Button_Close           关闭按钮
@Button_Buy_Item        购买物品按钮
@Button_Confirm         确认按钮
@Button_Cancel          取消按钮
```

#### Text文本
```
@Text_Title             标题文本
@Text_Description       描述文本
@Text_Player_Name       玩家名称
@Text_Gold_Count        金币数量
@Text_Level             等级
```

#### TextMeshPro
```
@TextTMP_Title          TMP标题
@TextTMP_Score          TMP分数
```

#### Image图片
```
@Image_Icon             图标
@Image_Avatar           头像
@Image_Background       背景
@Image_Item             物品图片
```

#### InputField输入框
```
@Input_Player_Name      玩家名输入
@Input_Search           搜索输入
@Input_Chat             聊天输入
```

#### Toggle开关
```
@Toggle_Sound           音效开关
@Toggle_Music           音乐开关
@Toggle_Auto_Play       自动播放开关
```

#### Slider滑动条
```
@Slider_Volume          音量滑动条
@Slider_Brightness      亮度滑动条
```

#### Transform容器
```
@Transform_Item_Container     物品容器
@Transform_Content            内容容器
```

#### GameObject对象
```
@GameObject_Panel             面板对象
@GameObject_Effects           特效对象
```

### 3.3 命名转换规则

| 标记 | 生成字段名 | 事件处理方法（Button） |
|------|-----------|---------------------|
| `@Button_Start` | `_startButton` | `OnStartClick()` |
| `@Button_Close_Panel` | `_closePanelButton` | `OnClosePanelClick()` |
| `@Text_Player_Name` | `_playerNameText` | - |
| `@Image_Icon` | `_iconImage` | - |

---

## 4. 文件命名

### 4.1 代码文件

```
MainMenuUI.cs              业务逻辑（手写）
MainMenuUI.Binding.cs      组件绑定（自动生成）
```

### 4.2 配置文件

```
UIManifest.asset           UI配置清单
```

---

## 5. 目录结构

### 5.1 推荐结构

```
Assets/
├─ Resources/
│  └─ UI/                         UI预制体
│     ├─ Main/                    主界面
│     │  ├─ MainMenu.prefab
│     │  └─ GamePlay.prefab
│     ├─ Popup/                   弹窗
│     │  ├─ Reward.prefab
│     │  └─ Confirm.prefab
│     └─ Common/                  通用组件
│        ├─ Loading.prefab
│        └─ Toast.prefab
│
├─ Game/Scripts/UI/               UI脚本
│  ├─ Main/
│  │  ├─ MainMenuUI.cs
│  │  ├─ MainMenuUI.Binding.cs
│  │  ├─ GamePlayUI.cs
│  │  └─ GamePlayUI.Binding.cs
│  ├─ Popup/
│  │  ├─ RewardUI.cs
│  │  ├─ RewardUI.Binding.cs
│  │  ├─ ConfirmUI.cs
│  │  └─ ConfirmUI.Binding.cs
│  └─ Common/
│     ├─ LoadingUI.cs
│     └─ LoadingUI.Binding.cs
│
└─ Resources/Config/
   └─ UIManifest.asset            UI配置清单
```

### 5.2 命名空间对应

```
Assets/Game/Scripts/UI/Main/     → namespace Game.UI.Main
Assets/Game/Scripts/UI/Popup/    → namespace Game.UI.Popup
Assets/Game/Scripts/UI/Common/   → namespace Game.UI.Common
```

---

## 📌 命名检查清单

在提交代码前，检查：

- [ ] Prefab使用帕斯卡命名法
- [ ] 组件标记包含@前缀
- [ ] 组件标记格式正确（@类型_名称）
- [ ] 多单词使用下划线分隔
- [ ] 文件名与类名一致
- [ ] 目录结构符合规范

---

## 🔧 命名规范检查工具

使用 `Tools -> UI工具 -> UI检查工具` 自动检查命名规范。

---

**文档版本**: v2.0  
**维护者**: GGFramework Team

