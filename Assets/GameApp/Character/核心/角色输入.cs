using UnityEngine;

namespace 游戏应用.角色
{
    /// <summary>
    /// 旧版 InputManager 输入适配，向控制器投递输入
    /// </summary>
    public class 角色输入 : MonoBehaviour
    {
        [Header("引用")]
        public 角色控制器 控制器;

        [Header("输入映射（旧版 InputManager）")]
        public string 水平轴 = "Horizontal";
        public string 垂直轴 = "Vertical";
        public KeyCode 跳跃键 = KeyCode.Space;
        public KeyCode 飞扑键 = KeyCode.LeftShift;
        public KeyCode 切换趴下键 = KeyCode.LeftControl;

        private void Reset()
        {
            控制器 = GetComponent<角色控制器>();
        }

        private void Update()
        {
            if (控制器 == null)
            {
                return;
            }

            角色输入数据 数据 = new 角色输入数据
            {
                平面移动输入 = new Vector2(Input.GetAxisRaw(水平轴), Input.GetAxisRaw(垂直轴)),
                跳跃按下 = Input.GetKeyDown(跳跃键),
                飞扑按下 = Input.GetKeyDown(飞扑键),
                切换趴下按下 = Input.GetKeyDown(切换趴下键),
            };

            控制器.设置输入(ref 数据);
        }
    }

    /// <summary>
    /// 输入数据结构（由输入组件组装）
    /// </summary>
    public struct 角色输入数据
    {
        public Vector2 平面移动输入; // x=right, y=forward
        public bool 跳跃按下;
        public bool 飞扑按下;
        public bool 切换趴下按下;
    }
}



