# Generations - 自动生成文件目录

> 本目录存放所有程序自动生成的文件

---

## 📁 目录结构

```
Generations/
├── Configs/                      # 生成的配置文件
│   ├── Excel/                    # Excel 生成的数据类
│   └── Proto/                    # Protobuf 生成的代码
│
├── Code/                         # 生成的代码
│   ├── UI/                       # UI 自动绑定代码
│   └── Data/                     # 数据访问代码
│
└── Manifests/                    # 资源清单
    └── AddressableResourceManifest.asset
```

---

## 📝 说明

### Manifests/ - 资源清单

**文件**：
- `AddressableResourceManifest.asset` - Addressable 资源清单

**生成方式**：
- Tools → 资源管理 → 生成 Addressable 清单

**用途**：
- 运行时快速查询资源位置
- 实现零开销资源检测（0ms）

### Configs/ - 生成的配置

**存放内容**：
- Excel 生成的数据类（.cs）
- Protobuf 生成的代码（.cs）
- 配置数据文件（.bytes）

### Code/ - 生成的代码

**存放内容**：
- UI 组件自动绑定代码
- 数据访问层代码
- 其他工具生成的代码

---

## ⚠️ 注意事项

1. **不要手动编辑** - 本目录文件都是自动生成的
2. **可忽略版本控制** - 可以将 Generations/ 加入 .gitignore（可选）
3. **定期重新生成** - 资源变化后需要重新生成清单

---

**本目录由 GGFramework 自动管理**

