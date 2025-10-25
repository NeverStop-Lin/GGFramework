using System.Collections.Generic;
using Defective.JSON;
using UnityEngine;

namespace Framework.Modules.Analytics
{
    /// <summary>
    /// 数据分析上报模块
    /// 提供通用的数据上报功能，包含常见的业务上报方�?
    /// </summary>
    public class AnalyticsModule
    {

        private List<BaseReporter> _reporters = new List<BaseReporter>();

        public AnalyticsModule()
        {
#if UNITY_EDITOR

#else
            _reporters.Add(new JsReporter());
#endif
            _reporters.ForEach(reporter =>
            {
                reporter.Initialize();
            });
        }


        protected void Report(string eventName, Dictionary<string, object> ext = null)
        {

            var json = new JSONObject();
            var typeKey = ".int";
            if (ext != null)
            {
                foreach (var keyValuePair in ext)
                {
                    if (keyValuePair.Key.EndsWith(typeKey))
                    {
                        var newKey = keyValuePair.Key;
                        newKey = newKey.Substring(0, newKey.Length - typeKey.Length);
                        json.AddField(newKey, (int)keyValuePair.Value);
                    }
                    else
                    {
                        json.AddField(keyValuePair.Key, (string)keyValuePair.Value);
                    }
                }
            }

            Debug.Log($"ReportManager Report：事�?{eventName} data:{json.ToString()}");
            _reporters.ForEach(reporter =>
            {
                reporter.Report(eventName, json);
            });
        }
        public void OnLoadEnter() { Report("load_enter"); }
        public void OnLoadLeave() { Report("load_leave"); }

        public void OnLevelEnter() { Report("level_enter"); }
        public void OnLevelLeave() { Report("level_leave"); }
        public void OnVideoEnter(string source)
        {
            Report("video_enter", new Dictionary<string, object>
            {
                {
                    "source", source
                }
            });
        }
        public void OnVideoLeave(string source, bool result)
        {
            Report("video_leave", new Dictionary<string, object>
            {
                {
                    "source", source
                },
                {
                    "result", result ? "成功" : "失败"
                }

            });
        }

        // ========== 以下是业务示例方法，可根据实际需求修改或删除 ==========

        // 示例：物品升级上�?
        public void OnItemLevelUp(int type, int id, int level)
        {
            Report("item_level_up", new Dictionary<string, object>
            {
                { "source", $"{type}_{id}" },
                { "item_level", $"{level}" }
            });
        }

        // 示例：钻石购买停止上�?
        public void OnDiamondStopBuy()
        {
            Report("diamond_stop_buy");
        }

        // 示例：场景购买上�?
        public void OnSceneBuy()
        {
            Report("scene_buy");
        }

    }
}