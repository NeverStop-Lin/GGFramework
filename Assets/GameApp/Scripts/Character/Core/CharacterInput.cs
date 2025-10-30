using UnityEngine;
using Framework.Core.Attributes;

namespace GameApp.Character
{
    /// <summary>
    /// 旧版 InputManager 输入适配，向控制器投递输入
    /// </summary>
    public class CharacterInput : MonoBehaviour
    {
        [Header("引用")]
        [Label("角色控制器")]
        public CharacterController Controller;

        [Header("输入映射（旧版 InputManager）")]
        [Label("水平轴")]
        public string HorizontalAxis = "Horizontal";
        [Label("垂直轴")]
        public string VerticalAxis = "Vertical";
        [Label("跳跃键")]
        public KeyCode JumpKey = KeyCode.Space;
        [Label("飞扑键")]
        public KeyCode DashKey = KeyCode.LeftShift;
        [Label("切换趴下键")]
        public KeyCode ToggleCrouchKey = KeyCode.LeftControl;

        private void Reset()
        {
            Controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (Controller == null)
            {
                return;
            }

            CharacterInputData data = new CharacterInputData
            {
                MoveInput = new Vector2(Input.GetAxisRaw(HorizontalAxis), Input.GetAxisRaw(VerticalAxis)),
                JumpDown = Input.GetKeyDown(JumpKey),
                DashDown = Input.GetKeyDown(DashKey),
                ToggleCrouchDown = Input.GetKeyDown(ToggleCrouchKey),
            };

            Controller.SetInput(ref data);
        }
    }

    /// <summary>
    /// 输入数据结构（由输入组件组装）
    /// </summary>
    public struct CharacterInputData
    {
        public Vector2 MoveInput; // x=right, y=forward
        public bool JumpDown;
        public bool DashDown;
        public bool ToggleCrouchDown;
    }
}



