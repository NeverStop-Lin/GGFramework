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
    public class ExcelConfigGenerator : EditorWindow
    {
        private const string EXCEL_ROOT = "Assets/../Excel/";
        private const string JSON_OUTPUT = "Assets/Resources/Configs/";
        private const string CSHARP_OUTPUT = "Assets/Generate/Scripts/Configs/";

        [MenuItem("Tools/打开Excel目录")]
        static void OpenExcelDirectory()
        {
            // 新增：打开Excel目录并选中
            if (Directory.Exists(EXCEL_ROOT))
            {
                EditorUtility.RevealInFinder(Path.GetFullPath(EXCEL_ROOT));
            }
            else
            {
                Directory.CreateDirectory(EXCEL_ROOT);
                AssetDatabase.Refresh();
                EditorUtility.RevealInFinder(Path.GetFullPath(EXCEL_ROOT));
            }
        }

        [MenuItem("Tools/Excel导出")]
        static void GenerateAll()
        {
            try
            {
                ClearOutputFolders();
                ProcessAllExcels();
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("生成完成", "配置代码和JSON数据已更�?", "确定");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("生成失败", e.Message, "关闭");
                Debug.LogError(e);
            }
        }

        static void ClearOutputFolders()
        {
            CleanDirectory(JSON_OUTPUT);
            CleanDirectory(CSHARP_OUTPUT);
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
            foreach (var file in Directory.GetFiles(EXCEL_ROOT, "*.xlsx", SearchOption.AllDirectories))
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
            var jsonPath = Path.Combine(JSON_OUTPUT, $"{data.ConfigName}.json");
            File.WriteAllText(jsonPath, JsonConvert.SerializeObject(jsonData, Formatting.Indented));
        }

        static void GenerateCSharpClass(SheetData data)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using Framework.Core.Systems.Config;");
            sb.AppendLine();
            sb.AppendLine("namespace Generate.Scripts.Configs");
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

            var csPath = Path.Combine(CSHARP_OUTPUT, $"{data.ConfigName}Configs.cs");
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