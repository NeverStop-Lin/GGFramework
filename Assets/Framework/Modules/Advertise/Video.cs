using System.Threading.Tasks;

namespace Framework.Modules.Advertise
{
    /// <summary>
    /// 视频广告显示选项
    /// </summary>
    public struct VideoShowOption
    {
        /// <summary>广告来源</summary>
        public string Source;

        public static implicit operator VideoShowOption(string source) =>
            new()
            {
                Source = source,
            };
    }

    /// <summary>
    /// 视频广告基类
    /// 项目可以继承此类实现具体平台的视频广�?
    /// </summary>
    public class Video
    {
        public void Initialize() { OnInitialize(); }
        
        /// <summary>
        /// 初始化广�?
        /// </summary>
        protected virtual void OnInitialize() { }
        
        /// <summary>
        /// 显示视频广告
        /// </summary>
        /// <param name="option">显示选项</param>
        /// <returns>是否观看成功</returns>
        public async Task<bool> Show(VideoShowOption option)
        {
            var result = await OnShow(option);
            return result;
        }

        /// <summary>
        /// 子类实现具体的广告显示逻辑
        /// </summary>
        /// <param name="option">显示选项</param>
        /// <returns>是否观看成功</returns>
        protected virtual Task<bool> OnShow(VideoShowOption option)
        {
            return Task.FromResult(true);
        }
    }
}