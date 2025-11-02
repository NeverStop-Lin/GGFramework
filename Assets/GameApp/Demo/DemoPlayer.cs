using UnityEngine;
using Cinemachine;

namespace GameApp.Demo
{
    /// <summary>
    /// Demo玩家输入处理脚本
    /// 收集玩家输入并传递给角色控制器
    /// </summary>
    public class DemoPlayer : MonoBehaviour
    {
        #region 常量
        private const string HORIZONTAL_INPUT = "Horizontal"; // 水平输入轴（A/D或←/→）
        private const string VERTICAL_INPUT = "Vertical";   // 垂直输入轴（W/S或↑/↓）
        #endregion

        #region 组件引用
        [Header("References")]
        public DemoCharacterController Character;
        public CinemachineBrain CameraBrain;
        #endregion

        #region Unity生命周期
        private void Start()
        {
            InitializeCameraBrain();
        }

        private void Update()
        {
            HandleCharacterInput();
        }
        #endregion

        #region 初始化
        /// <summary>
        /// 初始化相机大脑组件
        /// </summary>
        private void InitializeCameraBrain()
        {
            if (CameraBrain == null)
            {
                CameraBrain = FindObjectOfType<CinemachineBrain>();
            }

            if (CameraBrain == null)
            {
                Debug.LogWarning("CinemachineBrain not found. Camera rotation will be identity.");
            }
        }
        #endregion

        #region 输入处理
        /// <summary>
        /// 处理角色输入（每帧调用）
        /// </summary>
        private void HandleCharacterInput()
        {
            if (Character == null)
            {
                return;
            }

            DemoPlayerInputs inputs = CollectInputs();
            Character.SetInputs(ref inputs);
        }

        /// <summary>
        /// 收集玩家输入数据
        /// </summary>
        private DemoPlayerInputs CollectInputs()
        {
            DemoPlayerInputs inputs = new DemoPlayerInputs();

            inputs.MoveAxisForward = Input.GetAxisRaw(VERTICAL_INPUT);
            inputs.MoveAxisRight = Input.GetAxisRaw(HORIZONTAL_INPUT);
            inputs.CameraRotation = GetCameraRotation();
            inputs.JumpDown = Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Jump");
            inputs.DashDown = Input.GetKeyDown(KeyCode.LeftShift);

            return inputs;
        }

        /// <summary>
        /// 获取相机旋转
        /// </summary>
        private Quaternion GetCameraRotation()
        {
            if (CameraBrain != null && CameraBrain.OutputCamera != null)
            {
                return CameraBrain.OutputCamera.transform.rotation;
            }

            return Quaternion.identity;
        }
        #endregion
    }
}

