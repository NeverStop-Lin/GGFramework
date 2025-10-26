using Game.Manager;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// 玩家控制器
    /// 处理玩家移动、输入和交互
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("移动设置")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float gravity = -9.81f;
        
        [Header("交互设置")]
        [SerializeField] private float interactDistance = 3f;
        [SerializeField] private LayerMask interactLayer;

        private CharacterController _characterController;
        private Vector3 _verticalVelocity;
        private Transform _cameraTransform;
        
        private GameManager _gameManager;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _cameraTransform = Camera.main?.transform;
        }

        private void Start()
        {
            _gameManager = GameManager.Instance;
        }

        private void Update()
        {
            if (_gameManager == null || !_gameManager.IsGameRunning)
            {
                return;
            }

            HandleMovement();
            HandleInteraction();
        }

        /// <summary>
        /// 处理玩家移动
        /// </summary>
        private void HandleMovement()
        {
            // 获取输入
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            // 计算移动方向（相对于相机）
            Vector3 moveDirection = Vector3.zero;
            
            if (_cameraTransform != null)
            {
                Vector3 forward = _cameraTransform.forward;
                Vector3 right = _cameraTransform.right;
                
                forward.y = 0f;
                right.y = 0f;
                
                forward.Normalize();
                right.Normalize();
                
                moveDirection = forward * vertical + right * horizontal;
            }
            else
            {
                moveDirection = new Vector3(horizontal, 0, vertical);
            }

            // 应用移动
            if (moveDirection.magnitude >= 0.1f)
            {
                moveDirection.Normalize();
                _characterController.Move(moveDirection * moveSpeed * Time.deltaTime);
            }

            // 应用重力
            if (_characterController.isGrounded && _verticalVelocity.y < 0)
            {
                _verticalVelocity.y = -2f;
            }
            else
            {
                _verticalVelocity.y += gravity * Time.deltaTime;
            }

            _characterController.Move(_verticalVelocity * Time.deltaTime);
        }

        /// <summary>
        /// 处理玩家交互（采集资源）
        /// </summary>
        private void HandleInteraction()
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                TryInteract();
            }
        }

        /// <summary>
        /// 射线检测并执行交互
        /// </summary>
        private void TryInteract()
        {
            Ray ray = new Ray(transform.position + Vector3.up, transform.forward);
            
            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayer))
            {
                // 检查是否是资源对象
                var resourceObject = hit.collider.GetComponent<Resource.ResourceObject>();
                if (resourceObject != null)
                {
                    resourceObject.StartCollecting(this);
                }
            }
        }

        /// <summary>
        /// 获取玩家当前位置
        /// </summary>
        public Vector3 GetPosition()
        {
            return transform.position;
        }

        #region 调试绘制

        private void OnDrawGizmos()
        {
            // 绘制交互范围
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position + Vector3.up, transform.forward * interactDistance);
        }

        #endregion
    }
}
