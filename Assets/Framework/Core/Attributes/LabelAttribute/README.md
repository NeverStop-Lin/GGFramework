# Label 特性使用说明

## 功能说明

`[Label]` 特性用于在 Unity Inspector 面板中显示中文标签，支持两种显示模式，并可直接添加帮助文本。

## 使用方法

### 基础用法（双语模式，默认）

```csharp
using Framework.Core.Attributes;
using UnityEngine;

public class Example : MonoBehaviour
{
    // 只显示中文标签和英文字段名
    [Label("最大速度")]
    public float MaxSpeed = 10f;
}
```

**效果**：两行显示
```
最大速度    (加粗)
MaxSpeed   (淡灰色小字)
[字段值]
```

### 带帮助文本（推荐）

```csharp
// 第二个参数是帮助文本
[Label("跳起速度", "影响跳跃高度")]
public float JumpUpSpeed = 10f;

[Label("最大跳跃次数", "支持多段跳，1=单跳，2=二段跳")]
public int MaxJumpCount = 2;
```

**效果**：英文和帮助文本在同一行显示，支持自动换行
```
跳起速度                                    (加粗)
JumpUpSpeed | 影响跳跃高度                   (淡灰色小字)
[字段值]

最大跳跃次数                                 (加粗)
MaxJumpCount | 支持多段跳，1=单跳，2=二段跳   (淡灰色小字，长文本自动换行)
[字段值]
```

### 英文在上模式

第三个参数设为 `false` 可将英文字段名放在第一行（适合面向开发者的配置）。

```csharp
// 英文在上：适合开发者查看
[Label("最大速度", null, false)]
public float MaxSpeed = 10f;

// 英文在上 + 帮助文本
[Label("跳起速度", "影响跳跃高度", false)]
public float JumpUpSpeed = 10f;
```

**效果**：
```
MaxSpeed              (加粗) [10.0]
最大速度 | 影响跳跃高度  (淡灰色小字，鼠标悬停高亮)
```

## 参数说明

```csharp
[Label(label, help, showBilingual)]
```

- **label**（string，必需）：中文标签文本
- **help**（string，可选）：帮助文本，默认 null
- **showBilingual**（bool，可选）：显示顺序，默认 true
  - `true`（默认）：第一行中文（加粗），第二行英文+Help（小字）
  - `false`：第一行英文（加粗），第二行中文+Help（小字）

## 完整示例

```csharp
using Framework.Core.Attributes;
using UnityEngine;

public class CharacterConfig : ScriptableObject
{
    [Header("移动")]
    [Label("最大速度", "角色移动的速度上限")]
    public float MaxSpeed = 10f;
    
    [Label("加速度")]  // 无帮助文本
    public float Acceleration = 5f;
    
    [Header("跳跃")]
    [Label("跳跃力量", "影响跳跃高度", false)]  // 英文在上模式
    public float JumpForce = 8f;
}
```

## 注意事项

1. 需要引用 `using Framework.Core.Attributes;`
2. 适用于所有可序列化的字段
3. 与 `[Header]`、`[Range]`、`[Tooltip]` 等其他特性兼容
4. 固定两行布局，第二行单行显示（不自动换行）
5. 第三个参数控制中英文的显示顺序，不影响样式
6. 第二行占据整个宽度，文本用 `|` 分隔
7. 第二行文本为固定淡灰色，简洁高效

