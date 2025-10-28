using UnityEngine;
using Framework.Core;
using UnityEngine.UI;
using Framework.Scripts;

namespace Game.UI
{

    public class ShowModalUI : UIBehaviour
    {
        public GameObject _container;
        public Button _closeButton;
        public Button _confirmButton;


        protected override void OnCreate(params object[] args)
        {
            _closeButton.onClick.AddListener(OnCloseButtonClick);
            _confirmButton.onClick.AddListener(OnConfirmButtonClick);
        }

        private void OnCloseButtonClick()
        {
            Hide(false);
        }
        private void OnConfirmButtonClick()
        {
            Hide(true);
        }

    }
}
