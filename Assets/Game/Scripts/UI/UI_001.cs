using UnityEngine;
using Framework.Core;
using UnityEngine.UI;
using Framework.Scripts;

namespace Game.UI
{

    public class UI_001 : UIBehaviour
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
            message.text = message.text.ToString();
            Debug.Log("UI_001: " + message.gameObject.ToString());
            message.gameObject.SetActive(false);
            _timer.Reset();
            _timer.Delay(duration).Action(() => Hide()).Play();
        }
    }
}
