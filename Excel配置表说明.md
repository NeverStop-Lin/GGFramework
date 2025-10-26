# 生存游戏 - Excel配置表说明

## 创建步骤

1. 在项目根目录（`D:\GGFramework\`）创建 `Excel` 文件夹（如果不存在）
2. 在Excel文件夹中创建 `GameConfig.xlsx` 文件

## Excel表格结构

### Sheet 1: #Resource#资源配置

| 注释     | 资源ID | 资源名称 | 生命值 | 采集时间(秒) | 奖励类型 | 奖励数量 |
|---------|--------|---------|--------|------------|---------|---------|
| 类型     | int    | string  | int    | float      | string  | int     |
| 字段名   | ID     | Name    | Health | CollectTime| RewardType | RewardAmount |
| 数据示例 | 1      | 树木    | 100    | 2.0        | Wood    | 5       |
| 数据示例 | 2      | 石头    | 150    | 3.0        | Stone   | 3       |

### Sheet 2: #Enemy#敌人配置

| 注释     | 敌人ID | 敌人名称 | 最大生命值 | 移动速度 | 攻击伤害 | 攻击间隔(秒) |
|---------|--------|---------|----------|---------|---------|------------|
| 类型     | int    | string  | int      | float   | int     | float      |
| 字段名   | ID     | Name    | MaxHealth| Speed   | Damage  | AttackInterval |
| 数据示例 | 1      | 僵尸    | 50       | 2.0     | 10      | 1.5        |
| 数据示例 | 2      | 骷髅    | 30       | 3.0     | 8       | 1.0        |

## 导出配置

创建完Excel文件后，在Unity编辑器中：
1. 点击菜单 `Tools/Excel导出`
2. 等待生成完成
3. 会在以下位置生成文件：
   - `Assets/Resources/Configs/` - JSON配置数据
   - `Assets/Generate/Scripts/Configs/` - C#配置类

## 注意事项

- Sheet名称必须以 `#` 开头，格式：`#配置名#说明`
- 第一行：字段注释
- 第二行：字段类型（int/float/string/bool）
- 第三行：字段名称
- 第四行开始：数据内容
- 第一列必须是int类型的ID字段

