#if UNITY_EDITOR
namespace Framework.Editor.Core
{
    /// <summary>
    /// 框架默认路径配置
    /// 集中管理所有编辑器工具的默认路径和文件名
    /// </summary>
    public static class FrameworkDefaultPaths
    {
        #region 配置索引

        /// <summary>
        /// 配置索引文件路径（固定位置）
        /// </summary>
        public const string SettingsIndexPath = "Assets/Editor/FrameworkSettingsIndex.asset";

        #endregion

        #region 项目根目录
        /// <summary>
        /// 项目根目录 "Assets/Game"
        /// </summary>
        public const string ProjectRootFolder = "Assets/GameApp";

        #endregion

        #region UI管理器默认路径

        /// <summary>
        /// UI管理器配置默认保存目录
        /// </summary>
        public static string UIManagerSettingsFolder => $"{ProjectRootFolder}/Settings";

        /// <summary>
        /// UI管理器配置文件名
        /// </summary>
        public const string UIManagerSettingsFileName = "UIManagerSettings.asset";

        /// <summary>
        /// UI项目配置代码文件默认路径
        /// </summary>
        public static string UIProjectConfigCodePath => $"{ProjectRootFolder}/Settings/UIProjectConfigData.cs";

        /// <summary>
        /// UI预制体默认创建目录
        /// </summary>
        public static string UIPrefabFolder => $"{ProjectRootFolder}/Resources/UI";

        /// <summary>
        /// UI逻辑脚本默认输出目录
        /// </summary>
        public static string UILogicScriptFolder => $"{ProjectRootFolder}/Scripts/UI";

        /// <summary>
        /// UI代码默认命名空间 示例：Game.UI
        /// 说明：根据项目根目录自动生成，不需要手动设置
        /// 示例：Assets/Game -> Game.UI
        /// 示例：Assets/Game/Settings -> Game.Settings.UI
        /// </summary>
        public static string UIDefaultNamespace => $"{ProjectRootFolder.Replace("Assets/", "").Replace("/", ".")}.UI";

        #endregion

        #region UI默认分辨率配置

        /// <summary>
        /// 默认Canvas参考分辨率宽度
        /// </summary>
        public const int DefaultResolutionWidth = 1280;

        /// <summary>
        /// 默认Canvas参考分辨率高度
        /// </summary>
        public const int DefaultResolutionHeight = 720;

        /// <summary>
        /// 默认屏幕匹配模式（1=高度优先，适合横屏）
        /// </summary>
        public const float DefaultMatchWidthOrHeight = 1f;

        #endregion

        #region Excel生成器默认路径

        /// <summary>
        /// Excel生成器配置默认保存目录
        /// </summary>
        public static string ExcelGeneratorSettingsFolder => $"{ProjectRootFolder}/Settings";

        /// <summary>
        /// Excel生成器配置文件名
        /// </summary>
        public const string ExcelGeneratorSettingsFileName = "ExcelGeneratorSettings.asset";

        /// <summary>
        /// Excel文件默认根目录
        /// </summary>
        public const string ExcelRootFolder = "Assets/../Excel/";

        /// <summary>
        /// Excel生成JSON默认输出目录
        /// </summary>
        public static string ExcelJsonOutputFolder => $"{ProjectRootFolder}/Excel/Resources/Configs/";

        /// <summary>
        /// Excel生成C#代码默认输出目录
        /// </summary>
        public static string ExcelCSharpOutputFolder => $"{ProjectRootFolder}/Excel/Scripts/Configs/";

        /// <summary>
        /// Excel生成代码默认命名空间 示例：Game.Excel.Configs
        /// 说明：根据项目根目录自动生成，不需要手动设置
        /// 示例：Assets/Game -> Game.Configs
        /// 示例：Assets/Game/Excel -> Game.Excel.Configs
        /// </summary>
        public static string ExcelDefaultNamespace => $"{ProjectRootFolder.Replace("Assets/", "").Replace("/", ".")}.Excel.Configs";

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取UI管理器配置完整路径
        /// </summary>
        public static string GetUIManagerSettingsPath()
        {
            return $"{UIManagerSettingsFolder}/{UIManagerSettingsFileName}";
        }

        /// <summary>
        /// 获取Excel生成器配置完整路径
        /// </summary>
        public static string GetExcelGeneratorSettingsPath()
        {
            return $"{ExcelGeneratorSettingsFolder}/{ExcelGeneratorSettingsFileName}";
        }

        #endregion
    }
}
#endif

