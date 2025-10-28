# UI模板说明

## 目录用途

此目录用于存放UI预制体模板。每个模板是一个独立的文件夹，包含预制体和可选的UI类代码。

**重要**：此目录不在Editor文件夹内，因此模板代码可以作为运行时脚本挂载到预制体上。

## 模板结构

每个模板文件夹包含：
- **预制体文件**：与文件夹同名的 `.prefab` 文件（必需）
- **代码文件**：与文件夹同名的 `.cs` 文件（可选）

示例：
```
Assets/Framework/UITemplates/
  ├── DefaultUI/
  │   ├── DefaultUI.prefab    （必需）
  │   └── DefaultUI.cs        （可选）
  ├── PopupUI/
  │   ├── PopupUI.prefab
  │   └── PopupUI.cs
  └── TipsUI/
      └── TipsUI.prefab       （仅预制体，无代码）
```

## 默认模板

**DefaultUI**

包含以下组件：
- Canvas（渲染模式：Screen Space - Overlay）
- Canvas Scaler（Scale With Screen Size）
- Graphic Raycaster
- DefaultUI 脚本组件（继承自 UIBehaviour）

## 使用方式

1. 打开 `Tools > Framework > UI Manager`
2. 在"UI管理"Tab中点击"✚ 创建UI预制体"按钮
3. 在对话框中选择模板
4. 配置UI名称、保存目录、层级
5. 系统自动：
   - 基于模板创建预制体副本
   - 应用项目的Canvas Scaler配置
   - 如果模板有代码文件，复制代码并替换类名
   - 如果模板没有代码文件，生成默认UI代码
   - 自动挂载UI组件到预制体

## 添加自定义模板

### 方法1：带代码的模板

1. 在 `Templates` 目录下创建新文件夹，如 `PopupUI`
2. 创建预制体文件 `PopupUI.prefab`
   - 必须包含 Canvas 组件
   - 建议包含 Canvas Scaler 和 Graphic Raycaster
3. 创建代码文件 `PopupUI.cs`
   - 必须继承自 `UIBehaviour`
   - 类名必须与文件名一致
   - 创建新UI时，类名和命名空间会自动替换

### 方法2：仅预制体模板

1. 在 `Templates` 目录下创建新文件夹
2. 只创建预制体文件（与文件夹同名）
3. 创建UI时会自动生成默认代码

### 代码模板示例

```csharp
using UnityEngine;
using Framework.Core;

namespace Game.UI
{
    /// <summary>
    /// PopupUI 模板
    /// </summary>
    public class PopupUI : UIBehaviour
    {
        protected override void OnCreate(params object[] args)
        {
            // 初始化逻辑
        }

        protected override void OnShow(params object[] args)
        {
            // 显示逻辑
        }

        protected override void OnHide(params object[] args)
        {
            // 隐藏逻辑
        }
    }
}
```

## 模板扫描机制

- 每次打开创建对话框时自动扫描此目录
- 识别所有包含同名预制体的子文件夹
- 自动检测是否存在同名代码文件
- 支持多个模板同时存在
- 无需重启Unity或重新打开窗口

## 注意事项

- 模板文件夹名称、预制体名称、代码文件名称必须保持一致
- 如果模板代码存在，必须能正确解析出 `public class XXX` 类名
- 创建的UI预制体名称必须符合C#命名规范
- 如果默认模板被删除，系统会在下次打开UI管理器时自动重新生成
- 模板的命名空间会在创建新UI时自动替换为项目配置的命名空间

## 迁移说明

如果您从旧版本升级：
- 旧的 `Assets/Framework/Editor/UI/Template` 目录已废弃
- 新路径：`Assets/Framework/UITemplates/`（不在Editor文件夹内）
- 旧模板可以手动迁移：创建文件夹并将预制体移入，重命名为与文件夹同名

**为什么移出Editor文件夹？**
- Editor文件夹内的所有C#脚本会被Unity识别为编辑器脚本
- 编辑器脚本无法挂载到运行时GameObject上
- 模板代码需要能够挂载到预制体，因此必须放在非Editor目录

