using UnityEngine;
using UnityEngine.UI;

namespace Framework.CoreTools
{
    [AddComponentMenu("UIToos/Sprite", 0)]
    public class Sprite : Image
    {
        public enum CustomOption
        {
            Option1,
            Option2,
            Option3
        }

        [SerializeField]
        private CustomOption selectedOption = CustomOption.Option1;

        public CustomOption SelectedOption
        {
            get => selectedOption;
            set => selectedOption = value;
        }
    }
}