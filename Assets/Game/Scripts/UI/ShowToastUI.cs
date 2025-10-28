using UnityEngine;
using Framework.Core;
using UnityEngine.UI;
using Framework.Scripts;

namespace Game.UI
{

    public class ShowToastUI : UIBehaviour
    {
        public GameObject _container;
        public Text message;
        public float duration = 2f;
        Timer _timer;

        protected override void OnCreate(params object[] args)
        {
            _timer = GridFramework.Timer.GetOrCreate(this, "showToast");
        }

        protected override void OnShow(params object[] args)
        {
            message.text = args[0] as string;
            _timer.Reset();
            _timer.Delay(duration).Action(() => Hide()).Play();
        }
    }
}
