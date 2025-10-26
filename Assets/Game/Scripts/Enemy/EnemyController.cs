using Game.Manager;
using Game.Player;
using Generate.Scripts.Configs;
using UnityEngine;

namespace Game.Enemy
{
    /// <summary>
    /// 敌人控制器
    /// 简单的敌人AI：巡逻和追踪玩家
    /// </summary>
    public class EnemyController : MonoBehaviour
    {
        [Header("敌人配置")]
        [SerializeField] private int enemyId = 1;
        
        [Header("AI设置")]
        [SerializeField] private float detectionRange = 10f;
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float patrolRange = 5f;
        
        [Header("调试")]
        [SerializeField] private bool showDebugInfo = true;

        // 敌人状态
        private enum EnemyState
        {
            Patrol,
            Chase,
            Attack
        }

        private EnemyState _currentState = EnemyState.Patrol;
        private float _currentHealth;
        private PlayerController _targetPlayer;
        private Vector3 _initialPosition;
        private Vector3 _patrolTarget;
        private float _nextAttackTime;

        // 敌人配置数据
        private EnemyConfig _config;
        private GameManager _gameManager;

        private void Start()
        {
            _initialPosition = transform.position;
            _patrolTarget = GeneratePatrolPoint();
            
            LoadConfig();
            FindPlayer();
        }

        /// <summary>
        /// 从配置表加载敌人数据
        /// </summary>
        private void LoadConfig()
        {
            _gameManager = GameManager.Instance;
            if (_gameManager != null)
            {
                _config = _gameManager.GetEnemyConfig(enemyId);
                if (_config.ID != 0)
                {
                    _currentHealth = _config.MaxHealth;
                    if (showDebugInfo)
                    {
                        Debug.Log($"敌人加载成功：{_config.Name} (ID:{enemyId})");
                    }
                }
                else
                {
                    Debug.LogError($"找不到敌人配置：ID={enemyId}");
                }
            }
        }

        /// <summary>
        /// 查找玩家对象
        /// </summary>
        private void FindPlayer()
        {
            var playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                _targetPlayer = playerObject.GetComponent<PlayerController>();
            }
        }

        private void Update()
        {
            if (_gameManager == null || !_gameManager.IsGameRunning || _config.ID == 0)
            {
                return;
            }

            UpdateAI();
        }

        /// <summary>
        /// 更新AI逻辑
        /// </summary>
        private void UpdateAI()
        {
            if (_targetPlayer == null)
            {
                FindPlayer();
                return;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, _targetPlayer.GetPosition());

            // 状态切换
            switch (_currentState)
            {
                case EnemyState.Patrol:
                    if (distanceToPlayer <= detectionRange)
                    {
                        ChangeState(EnemyState.Chase);
                    }
                    else
                    {
                        DoPatrol();
                    }
                    break;

                case EnemyState.Chase:
                    if (distanceToPlayer <= attackRange)
                    {
                        ChangeState(EnemyState.Attack);
                    }
                    else if (distanceToPlayer > detectionRange * 1.5f)
                    {
                        ChangeState(EnemyState.Patrol);
                    }
                    else
                    {
                        DoChase();
                    }
                    break;

                case EnemyState.Attack:
                    if (distanceToPlayer > attackRange * 1.2f)
                    {
                        ChangeState(EnemyState.Chase);
                    }
                    else
                    {
                        DoAttack();
                    }
                    break;
            }
        }

        /// <summary>
        /// 切换状态
        /// </summary>
        private void ChangeState(EnemyState newState)
        {
            if (_currentState == newState) return;

            _currentState = newState;
            
            if (showDebugInfo)
            {
                Debug.Log($"{_config.Name} 切换到状态：{newState}");
            }
        }

        /// <summary>
        /// 执行巡逻
        /// </summary>
        private void DoPatrol()
        {
            // 移动到巡逻点
            Vector3 direction = (_patrolTarget - transform.position).normalized;
            transform.position += direction * _config.Speed * Time.deltaTime;
            
            // 面向移动方向
            if (direction != Vector3.zero)
            {
                transform.forward = direction;
            }

            // 到达巡逻点，生成新的巡逻点
            if (Vector3.Distance(transform.position, _patrolTarget) < 1f)
            {
                _patrolTarget = GeneratePatrolPoint();
            }
        }

        /// <summary>
        /// 执行追踪
        /// </summary>
        private void DoChase()
        {
            Vector3 direction = (_targetPlayer.GetPosition() - transform.position).normalized;
            transform.position += direction * _config.Speed * Time.deltaTime;
            
            // 面向玩家
            if (direction != Vector3.zero)
            {
                transform.forward = direction;
            }
        }

        /// <summary>
        /// 执行攻击
        /// </summary>
        private void DoAttack()
        {
            // 面向玩家
            Vector3 direction = (_targetPlayer.GetPosition() - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                transform.forward = direction;
            }

            // 攻击冷却
            if (Time.time >= _nextAttackTime)
            {
                AttackPlayer();
                _nextAttackTime = Time.time + _config.AttackInterval;
            }
        }

        /// <summary>
        /// 攻击玩家
        /// </summary>
        private void AttackPlayer()
        {
            if (_gameManager != null)
            {
                _gameManager.PlayerData.TakeDamage(_config.Damage);
                
                if (showDebugInfo)
                {
                    Debug.Log($"{_config.Name} 攻击玩家，造成 {_config.Damage} 点伤害");
                }
            }
        }

        /// <summary>
        /// 生成随机巡逻点
        /// </summary>
        private Vector3 GeneratePatrolPoint()
        {
            Vector2 randomPoint = Random.insideUnitCircle * patrolRange;
            return _initialPosition + new Vector3(randomPoint.x, 0, randomPoint.y);
        }

        /// <summary>
        /// 受到伤害（预留接口）
        /// </summary>
        public void TakeDamage(float damage)
        {
            _currentHealth -= damage;
            
            if (showDebugInfo)
            {
                Debug.Log($"{_config.Name} 受到 {damage} 点伤害，剩余生命值：{_currentHealth}");
            }

            if (_currentHealth <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// 死亡
        /// </summary>
        private void Die()
        {
            if (showDebugInfo)
            {
                Debug.Log($"{_config.Name} 已死亡");
            }
            
            Destroy(gameObject);
        }

        #region 调试绘制

        private void OnDrawGizmosSelected()
        {
            if (!showDebugInfo) return;

            // 绘制检测范围
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            // 绘制攻击范围
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            // 绘制巡逻范围
            Gizmos.color = Color.blue;
            Vector3 center = Application.isPlaying ? _initialPosition : transform.position;
            Gizmos.DrawWireSphere(center, patrolRange);

            // 绘制巡逻目标点
            if (Application.isPlaying)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(_patrolTarget, 0.5f);
                Gizmos.DrawLine(transform.position, _patrolTarget);
            }
        }

        #endregion
    }
}
