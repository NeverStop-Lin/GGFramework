# UI模板说明

## 目录用途

此目录用于存放UI预制体模板。模板会在首次打开UI管理器时自动生成。

## 默认模板

**DefaultUITemplate.prefab**

包含以下组件：
- Canvas（渲染模式：Screen Space - Overlay）
- Canvas Scaler（Scale With Screen Size）
- Graphic Raycaster

## 使用方式

1. 打开 `Tools > Framework > UI Manager`
2. 在"UI管理"Tab中点击"✚ 创建UI预制体"按钮
3. 在对话框中选择模板
4. 配置UI名称、保存目录、层级
5. 系统自动基于选择的模板创建预制体副本，并应用项目的Canvas Scaler配置

## 添加自定义模板

框架开发者可以轻松添加更多模板：

1. **直接添加预制体**
   - 在此目录下创建新的预制体文件
   - 建议命名格式：`XxxTemplate.prefab`（如 `PopupTemplate.prefab`）
   - 系统会自动扫描并在创建对话框中显示
   - 显示名称会自动去掉"Template"后缀

2. **模板命名规范**
   - `DefaultUITemplate.prefab` - 默认模板
   - `PopupTemplate.prefab` - 显示为"Popup"
   - `FullScreenTemplate.prefab` - 显示为"FullScreen"
   - `TipsTemplate.prefab` - 显示为"Tips"

3. **模板内容**
   - 必须包含 Canvas 组件
   - 建议包含 Canvas Scaler 和 Graphic Raycaster
   - 创建时会应用项目配置的参考分辨率

## 模板扫描机制

- 每次打开创建对话框时自动扫描此目录
- 只扫描 `.prefab` 文件
- 支持多个模板同时存在
- 无需重启Unity或重新打开窗口

## 注意事项

- 模板会在创建UI时自动应用项目配置的参考分辨率
- 创建的UI预制体名称必须符合C#命名规范
- 如果模板被删除，系统会在下次打开UI管理器时自动重新生成

