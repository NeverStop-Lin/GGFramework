using Framework.Core;
using UnityEngine;
using Zenject;

/// <summary>
/// 游戏输入系统
/// </summary>
public class GamePlayInput : IInitializable
{
    [Inject] private IObservers _observers;

    /// <summary>  移动玩家方向 Observer  </summary>
    public IValueObserver<Vector2> PlayerMoveInputObserver { get; private set; }

    /// <summary> 旋转相机增量值 Observer </summary>
    public IValueObserver<Vector2> CameraRotateInputObserver { get; private set; }

    /// <summary> 是否触摸旋转相机区域 Observer </summary>
    public IValueObserver<bool> IsTouchCameraRotateAreaObserver { get; private set; }

    public static int Value = 1;

    [Inject]
    public void Initialize()
    {
        PlayerMoveInputObserver = _observers.Value(Vector2.zero);
        CameraRotateInputObserver = _observers.Value(Vector2.zero);
        IsTouchCameraRotateAreaObserver = _observers.Value(false);
    }
}
