using System.Collections;
using System.Collections.Generic;
using Framework.Scripts;
using Game.UI;
using UnityEngine;
namespace Framework.Scripts
{
    public partial class Launcher
    {
        // Start is called before the first frame update
        void Start()
        {
            GridFramework.UI.Show<MainMenuUI>();
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}

