
using UnityEngine;


/// <summary>
/// 游戏输入系统
/// </summary>
public class GamePlayInput
{
    /// <summary>
    /// 移动玩家方向
    /// </summary>
    public Vector3 PlayerMoveInput { get; private set; }
    /// <summary>
    /// 旋转相机增量值
    /// </summary>
    public Vector2 CameraRotateInput { get; private set; }
    /// <summary>
    /// 是否触摸旋转相机区域
    /// </summary>
    public bool IsTouchCameraRotateArea { get; private set; }


    /// <summary>
    /// 设置移动玩家方向
    /// </summary>
    /// <param name="direction">移动方向</param>
    public void SetMoveDirection(Vector2 direction)
    {
        PlayerMoveInput = new Vector3(direction.x, 0, direction.y);
    }

    /// <summary>
    /// 设置旋转相机增量值
    /// </summary>
    /// <param name="direction">旋转方向</param>
    public void SetCameraRotateDelta(Vector2 delta)
    {
        CameraRotateInput = delta;
    }

    /// <summary>
    /// 设置是否触摸旋转相机区域
    /// </summary>
    /// <param name="isTouch">是否触摸</param>
    public void SetIsTouchCameraRotateArea(bool isTouch)
    {
        IsTouchCameraRotateArea = isTouch;
    }
}