using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ExcelDataReader;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Framework.Editor.Excel
{
    public class ExcelConfigGenerator
    {
        /// <summary>
        /// 获取Excel根目录路径
        /// </summary>
        private static string GetExcelRoot()
        {
            return ExcelGeneratorSettings.Instance?.ExcelRootPath ?? Core.FrameworkDefaultPaths.ExcelRootFolder;
        }
        
        /// <summary>
        /// 获取JSON输出路径
        /// </summary>
        private static string GetJsonOutput()
        {
            return ExcelGeneratorSettings.Instance?.JsonOutputPath ?? Core.FrameworkDefaultPaths.ExcelJsonOutputFolder;
        }
        
        /// <summary>
        /// 获取C#代码输出路径
        /// </summary>
        private static string GetCSharpOutput()
        {
            return ExcelGeneratorSettings.Instance?.CSharpOutputPath ?? Core.FrameworkDefaultPaths.ExcelCSharpOutputFolder;
        }
        
        /// <summary>
        /// 获取默认命名空间
        /// </summary>
        private static string GetNamespace()
        {
            return ExcelGeneratorSettings.Instance?.DefaultNamespace ?? Core.FrameworkDefaultPaths.ExcelDefaultNamespace;
        }

        [MenuItem("Framework/Excel/快速导出")]
        public static void QuickGenerate()
        {
            // 检查配置
            if (!CheckConfigurationForQuickExport())
                return;
            
            // 配置完整，执行导出
            GenerateAll();
        }
        
        /// <summary>
        /// 检查配置是否完整（用于快速导出）
        /// 如果配置不完整，会打开主界面让用户配置
        /// </summary>
        private static bool CheckConfigurationForQuickExport()
        {
            if (!Core.FrameworkSettingsIndex.Exists())
            {
                Core.FrameworkSettingsIndex.GetOrCreate();
            }
            
            var settings = ExcelGeneratorSettings.Instance;
            if (settings == null)
            {
                // 配置不存在，显示欢迎窗口
                Core.SettingsWelcomeWindow.ShowExcelGeneratorWelcome();
                return false;
            }
            
            if (!settings.IsInitialized())
            {
                // 配置未初始化，打开主界面让用户配置
                EditorUtility.DisplayDialog("配置不完整", 
                    "Excel生成器尚未配置完成\n请在打开的窗口中完成必填项的配置", 
                    "确定");
                ExcelGeneratorWindow.ShowWindow();
                return false;
            }
            
            return true;
        }

        public static void GenerateAll()
        {
            try
            {
                ClearOutputFolders();
                ProcessAllExcels();
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("生成完成", "配置代码和JSON数据已更新", "确定");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("生成失败", e.Message, "关闭");
                Debug.LogError(e);
            }
        }
        
        /// <summary>
        /// 从单个Excel文件生成配置
        /// 不清空输出目录，只更新该文件对应的配置
        /// </summary>
        public static void GenerateFromFile(string excelFilePath)
        {
            if (string.IsNullOrEmpty(excelFilePath))
            {
                throw new System.ArgumentException("Excel文件路径不能为空", nameof(excelFilePath));
            }
            
            if (!File.Exists(excelFilePath))
            {
                throw new System.IO.FileNotFoundException($"Excel文件不存在: {excelFilePath}");
            }
            
            try
            {
                ProcessExcelFile(excelFilePath);
                AssetDatabase.Refresh();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"处理Excel文件失败: {excelFilePath}\n{e.Message}");
                throw;
            }
        }

        static void ClearOutputFolders()
        {
            CleanDirectory(GetJsonOutput());
            CleanDirectory(GetCSharpOutput());
        }

        static void CleanDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
                FileUtil.DeleteFileOrDirectory(path);
            }
            Directory.CreateDirectory(path);
        }

        static void ProcessAllExcels()
        {
            var excelRoot = GetExcelRoot();
            
            // 将相对路径转换为绝对路径
            var fullPath = excelRoot;
            if (excelRoot.StartsWith("Assets/"))
            {
                fullPath = Path.Combine(Application.dataPath, excelRoot.Substring("Assets/".Length));
            }
            else if (excelRoot.StartsWith("Assets/../"))
            {
                fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", excelRoot.Substring("Assets/../".Length)));
            }
            
            if (!Directory.Exists(fullPath))
            {
                throw new DirectoryNotFoundException($"Excel根目录不存在: {fullPath}");
            }
            
            foreach (var file in Directory.GetFiles(fullPath, "*.xlsx", SearchOption.AllDirectories))
            {
                if (Path.GetFileName(file).StartsWith("~")) continue;
                ProcessExcelFile(file);
            }
        }

        static void ProcessExcelFile(string excelPath)
        {
            using (var stream = new FileStream(excelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                do
                {
                    var sheetName = reader.Name;
                    if (!sheetName.StartsWith("#")) continue;

                    var nameParts = sheetName.TrimStart('#').Split(new[]
                    {
                        '#'
                    }, 2);
                    var configName = nameParts[0];
                    var configDesc = nameParts.Length > 1 ? nameParts[1] : "";

                    var sheetData = new SheetData
                    {
                        ConfigName = configName,
                        ConfigDesc = configDesc,
                        Comments = ReadRow(reader),
                        Types = ReadRow(reader),
                        Fields = ReadRow(reader)
                    };

                    GenerateConfigFiles(reader, sheetData);
                } while (reader.NextResult());
            }
        }

        static List<string> ReadRow(IExcelDataReader reader)
        {
            if (!reader.Read()) return new List<string>();
            return Enumerable.Range(0, reader.FieldCount).Select(i => reader.GetValue(i)?.ToString()?.Trim() ?? "")
                .ToList();
        }

        static void GenerateConfigFiles(IExcelDataReader reader, SheetData data)
        {
            var jsonData = new List<Dictionary<string, object>>();
            while (reader.Read())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < data.Fields.Count; i++)
                {
                    var value = ConvertValue(reader.GetValue(i), data.Types.Count > i ? data.Types[i] : "string");
                    row[data.Fields[i]] = value;
                }
                jsonData.Add(row);
            }

            SaveJsonFile(data, jsonData);
            GenerateCSharpClass(data);
        }

        static object ConvertValue(object value, string type)
        {
            if (value == null)
                return type switch
                {
                    "int" => 0,
                    "float" => 0f,
                    "bool" => false,
                    _ => ""
                };

            try
            {
                return type.ToLower() switch
                {
                    "int" => System.Convert.ToInt32(value),
                    "float" => System.Convert.ToSingle(value),
                    "bool" => System.Convert.ToBoolean(value),
                    _ => value.ToString()
                };
            }
            catch
            {
                return type switch
                {
                    "int" => 0,
                    "float" => 0f,
                    "bool" => false,
                    _ => ""
                };
            }
        }

        static void SaveJsonFile(SheetData data, object jsonData)
        {
            var jsonPath = Path.Combine(GetJsonOutput(), $"{data.ConfigName}.json");
            File.WriteAllText(jsonPath, JsonConvert.SerializeObject(jsonData, Formatting.Indented));
        }

        static void GenerateCSharpClass(SheetData data)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using Framework.Core;");
            sb.AppendLine();
            sb.AppendLine($"namespace {GetNamespace()}");
            sb.AppendLine("{");
            sb.AppendLine($"\t/// <summary>");
            sb.AppendLine($"\t/// {data.ConfigDesc}");
            sb.AppendLine($"\t/// </summary>");
            sb.AppendLine($"\tpublic struct {data.ConfigName}Config");
            sb.AppendLine("\t{");

            for (int i = 0; i < data.Fields.Count; i++)
            {
                var comment = data.Comments.Count > i ? data.Comments[i] : "";
                sb.AppendLine($"\t\t/// <summary> {comment} </summary>");
                sb.AppendLine($"\t\tpublic {ConvertType(data.Types[i])} {data.Fields[i]};");
                sb.AppendLine();
            }

            sb.AppendLine("\t}");
            sb.AppendLine();
            if (data.Types[0].StartsWith("int"))
            {
                sb.AppendLine($"\tpublic class {data.ConfigName}Configs : BaseConfig<List<{data.ConfigName}Config>>");
            }
            else
            {
                sb.AppendLine($"\tpublic class {data.ConfigName}Configs : BaseConfig<{data.ConfigName}Config>");
            }

            sb.AppendLine("\t{");
            sb.AppendLine($"\t\tpublic override string Url => $\"Configs/{data.ConfigName}\";");
            sb.AppendLine("\t}");
            sb.AppendLine("}");

            var csPath = Path.Combine(GetCSharpOutput(), $"{data.ConfigName}Configs.cs");
            File.WriteAllText(csPath, sb.ToString());
        }

        static string ConvertType(string excelType)
        {
            return excelType.ToLower() switch
            {
                "int" => "int",
                "float" => "float",
                "bool" => "bool",
                _ => "string"
            };
        }

        class SheetData
        {
            public string ConfigName;
            public string ConfigDesc;
            public List<string> Comments;
            public List<string> Types;
            public List<string> Fields;
        }
    }
}