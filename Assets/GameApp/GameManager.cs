using Framework.Scripts;
using GameApp.UI;
using UnityEngine;
using Zenject;

public class GameManager : MonoInstaller
{

    public override void InstallBindings()
    {

        Container.Bind<GamePlayInput>().AsSingle();

    }

    public override void Start()
    {
        GGF.UI.Show<GameHUD>();
    }

}