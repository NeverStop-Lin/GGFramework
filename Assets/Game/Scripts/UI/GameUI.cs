using UnityEngine;
using Framework.Core;
using Framework.Scripts;

namespace Game.UI
{
    /// <summary>
    /// GameUI - 业务逻辑
    /// </summary>
    public partial class GameUI
    {
        #region UI配置

        /// <summary>
        /// 创建UI配置
        /// </summary>
        protected override UIConfig CreateUIConfig()
        {
            return new UIConfig
            {
                ResourcePath = "UI/GameUI",
                UIType = UIType.Main,
                AlignType = UIAlignType.Center,
                CacheStrategy = UICacheStrategy.AlwaysCache,
                UseAnimation = false
            };
        }

        #endregion

        #region 生命周期

        /// <summary>
        /// 显示UI时调用
        /// </summary>
        protected override void OnShow(params object[] args)
        {
            base.OnShow(args);

            // TODO: 实现显示逻辑
        }

        /// <summary>
        /// 隐藏UI时调用
        /// </summary>
        protected override void OnHide(params object[] args)
        {
            // TODO: 实现隐藏逻辑

            base.OnHide(args);
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// Start 按钮点击
        /// </summary>
        private void OnStartClick()
        {
            // TODO: 实现点击逻辑
            GridFramework.UI.Show<MainMenuUI>();
            Hide();
        }

        /// <summary>
        /// Settings 按钮点击
        /// </summary>
        private void OnSettingsClick()
        {
            // TODO: 实现点击逻辑
            Debug.Log("当前是GameUI");
        }

        #endregion
    }
}
