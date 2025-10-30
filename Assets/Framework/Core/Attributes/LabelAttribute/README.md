# Label 特性使用说明

## 功能说明

`[Label]` 特性用于在 Unity Inspector 面板中显示中文标签，同时鼠标悬停时显示英文字段名。

## 使用方法

```csharp
using Framework.Core;
using UnityEngine;

public class Example : MonoBehaviour
{
    [Label("最大速度")]
    public float MaxSpeed = 10f;
    
    [Label("跳跃力量")]
    [Tooltip("可以和 Tooltip 一起使用添加额外说明")]
    public float JumpForce = 5f;
}
```

## 效果

- **Inspector 显示**：`最大速度` (中文)
- **鼠标悬停提示**：`MaxSpeed` (英文字段名)
- **如果有 Tooltip**：同时显示 Tooltip 内容

## 注意事项

1. 需要引用 `using Framework.Core;`
2. 适用于所有可序列化的字段
3. 与 `[Header]`、`[Range]` 等其他特性兼容

