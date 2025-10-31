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

        [Inject] protected GamePlayInput GamePlayInput;
        public TouchDragInput TouchDragInput;
        public VirtualJoystick VirtualJoystick;


        protected override void OnCreate(params object[] args)
        {
            TouchDragInput.OnDragDeltaChanged.Add(val => GamePlayInput.SetCameraRotateDelta(new Vector2(val.x, val.y)), this, false);
            TouchDragInput.OnIsDraggingChanged.Add(val => GamePlayInput.SetIsTouchCameraRotateArea(val), this, false);
            VirtualJoystick.OnInputChanged.Add(val => GamePlayInput.SetMoveDirection(new Vector2(val.x, val.y)), this, false);
        }

        protected override void OnShow(params object[] args)
        {

        }

        protected override object OnHide(params object[] args)
        {
            return null;
        }

        protected override void OnRemove(params object[] args)
        {
            TouchDragInput.OnDragDeltaChanged.RemoveTarget(this);
            TouchDragInput.OnIsDraggingChanged.RemoveTarget(this);
            VirtualJoystick.OnInputChanged.RemoveTarget(this);
        }
    }
}
