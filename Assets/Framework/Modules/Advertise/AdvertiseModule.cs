namespace Framework.Modules.Advertise
{
    /// <summary>
    /// 广告管理模块
    /// 提供视频广告等广告功�?
    /// </summary>
    public class AdvertiseModule
    {
        /// <summary>视频广告</summary>
        public Video Video;

        public AdvertiseModule()
        {
            // 默认使用基础Video类，项目可以继承Video类来实现具体平台的广�?
            Video = new Video();
            Video.Initialize();
        }
    }
}