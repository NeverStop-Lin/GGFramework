using UnityEngine;
using Framework.Core;
using UnityEngine.UI;
using System.Collections;

namespace Game.UI
{

    public class ShowToastUI : UIBehaviour
    {

        public GameObject container;
        public Text message;
        public float duration = 2f;


        protected override void OnCreate(params object[] args)
        {
        }

        protected override void OnShow(params object[] args)
        {
            message.text = args[0] as string;
            StartCoroutine(HideAfterDelay());
        }
        IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(duration);
            Remove();
        }
        protected override void OnHide(params object[] args)
        {

        }

        private void Update()
        {

        }
    }
}
