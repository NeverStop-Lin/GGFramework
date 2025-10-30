using UnityEngine;
using Framework.Core;
using Framework.Scripts;
using Zenject;

namespace GameApp.UI
{
    /// <summary>
    /// GameHUD 模板
    /// </summary>
    public class GameHUD : UIBehaviour
    {

        [Inject] GamePlayInput GamePlayInput;

        public TouchDragInput TouchDragInput;
        public VirtualJoystick VirtualJoystick;

        protected override void OnShow(params object[] args)
        {

        }

        protected override object OnHide(params object[] args)
        {
            return null;
        }

        private void Update()
        {
            if (VirtualJoystick != null && VirtualJoystick.IsDragging)
            {
                GamePlayInput.SetMoveDirection(VirtualJoystick.Input);
            }
            if (TouchDragInput != null)
            {
                if (TouchDragInput.IsDragging)
                    GamePlayInput.SetCameraRotateDelta(TouchDragInput.DragDelta);
                GamePlayInput.SetIsTouchCameraRotateArea(TouchDragInput.IsDragging);
            }
        }
    }
}
