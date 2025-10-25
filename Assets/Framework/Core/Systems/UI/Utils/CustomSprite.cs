namespace Framework.CoreTools
{
    using UnityEngine;
    using UnityEngine.UI;

    [AddComponentMenu("UI/Custom Sprite", 10)]
    public class CustomSprite : Image // 确保继承自Image
    {
        public enum SpriteOption
        {
            Default,
            PixelArt,
            Rounded
        }

        [SerializeField]
        private SpriteOption currentOption = SpriteOption.Default;

        // 属性变化时的逻辑（可选）
        public SpriteOption CurrentOption
        {
            get => currentOption;
            set
            {
                currentOption = value;
                ApplyOptionEffects();
            }
        }

        public void ApplyOptionEffects() { }

#if UNITY_EDITOR
        // 在编辑器发生变化时自动执�?
        protected override void OnValidate()
        {
            base.OnValidate();
            ApplyOptionEffects();
        }
#endif
    }
}