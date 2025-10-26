using Game.Manager;
using Game.Player;
using Generate.Scripts.Configs;
using UnityEngine;

namespace Game.Resource
{
    /// <summary>
    /// 资源对象
    /// 可采集的资源（树木、石头等）
    /// </summary>
    public class ResourceObject : MonoBehaviour
    {
        [Header("资源配置")]
        [SerializeField] private int resourceId = 1;
        
        [Header("显示设置")]
        [SerializeField] private GameObject progressBarPrefab;
        
        private int _currentHealth;
        private bool _isCollecting;
        private float _collectProgress;
        private PlayerController _collector;
        
        // 资源配置数据（从Excel读取）
        private ResourceConfig _config;
        
        private GameObject _progressBarInstance;
        private UnityEngine.UI.Slider _progressSlider;

        private void Start()
        {
            LoadConfig();
        }

        /// <summary>
        /// 从配置表加载资源数据
        /// </summary>
        private void LoadConfig()
        {
            var manager = GameManager.Instance;
            if (manager != null)
            {
                _config = manager.GetResourceConfig(resourceId);
                if (_config.ID != 0)
                {
                    _currentHealth = _config.Health;
                    Debug.Log($"资源对象加载成功：{_config.Name} (ID:{resourceId})");
                }
                else
                {
                    Debug.LogError($"找不到资源配置：ID={resourceId}");
                }
            }
        }

        /// <summary>
        /// 开始采集
        /// </summary>
        public void StartCollecting(PlayerController player)
        {
            if (_isCollecting || _config.ID == 0) return;
            
            _isCollecting = true;
            _collectProgress = 0f;
            _collector = player;
            
            ShowProgressBar();
            
            Debug.Log($"开始采集：{_config.Name}");
        }

        private void Update()
        {
            if (_isCollecting)
            {
                ProcessCollecting();
            }
        }

        /// <summary>
        /// 处理采集逻辑
        /// </summary>
        private void ProcessCollecting()
        {
            // 检查玩家是否按住E键
            if (!Input.GetKey(KeyCode.E))
            {
                CancelCollecting();
                return;
            }
            
            // 检查玩家距离
            if (_collector != null)
            {
                float distance = Vector3.Distance(transform.position, _collector.GetPosition());
                if (distance > 3.5f)
                {
                    CancelCollecting();
                    return;
                }
            }
            
            // 增加采集进度
            _collectProgress += Time.deltaTime;
            UpdateProgressBar(_collectProgress / _config.CollectTime);
            
            // 采集完成
            if (_collectProgress >= _config.CollectTime)
            {
                CompleteCollecting();
            }
        }

        /// <summary>
        /// 取消采集
        /// </summary>
        private void CancelCollecting()
        {
            _isCollecting = false;
            _collectProgress = 0f;
            _collector = null;
            
            HideProgressBar();
            
            Debug.Log("采集已取消");
        }

        /// <summary>
        /// 完成采集
        /// </summary>
        private void CompleteCollecting()
        {
            _isCollecting = false;
            
            // 给予奖励
            var manager = GameManager.Instance;
            if (manager != null)
            {
                manager.PlayerData.AddResource(_config.RewardType, _config.RewardAmount);
            }
            
            HideProgressBar();
            
            // 扣除生命值
            _currentHealth -= 10; // 每次采集扣10点
            
            if (_currentHealth <= 0)
            {
                DestroyResource();
            }
            else
            {
                Debug.Log($"{_config.Name} 剩余生命值：{_currentHealth}");
            }
        }

        /// <summary>
        /// 销毁资源对象
        /// </summary>
        private void DestroyResource()
        {
            Debug.Log($"{_config.Name} 已耗尽");
            Destroy(gameObject);
        }

        #region 进度条显示

        private void ShowProgressBar()
        {
            if (_progressBarInstance == null && progressBarPrefab != null)
            {
                _progressBarInstance = Instantiate(progressBarPrefab, transform.position + Vector3.up * 2f, Quaternion.identity);
                _progressBarInstance.transform.SetParent(transform);
                _progressSlider = _progressBarInstance.GetComponentInChildren<UnityEngine.UI.Slider>();
            }
            
            if (_progressBarInstance != null)
            {
                _progressBarInstance.SetActive(true);
            }
        }

        private void UpdateProgressBar(float progress)
        {
            if (_progressSlider != null)
            {
                _progressSlider.value = progress;
            }
        }

        private void HideProgressBar()
        {
            if (_progressBarInstance != null)
            {
                _progressBarInstance.SetActive(false);
            }
        }

        #endregion
    }
}
