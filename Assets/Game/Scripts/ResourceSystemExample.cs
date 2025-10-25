using System.Collections;
using Framework.Scripts;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// 资源系统使用示例
    /// </summary>
    public class ResourceSystemExample : MonoBehaviour
    {
        void Start()
        {
            Debug.Log("===== GGFramework 资源系统示例 =====");
            Debug.Log("按键说明:");
            Debug.Log("1 - 加载资源");
            Debug.Log("2 - 批量预加载");
            Debug.Log("3 - 对象池示例");
            Debug.Log("4 - 查看资源状态");
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) ExampleLoadResource();
            if (Input.GetKeyDown(KeyCode.Alpha2)) ExamplePreload();
            if (Input.GetKeyDown(KeyCode.Alpha3)) ExampleObjectPool();
            if (Input.GetKeyDown(KeyCode.Alpha4)) ExampleResourceStatus();
        }

        // 示例1：加载资源
        async void ExampleLoadResource()
        {
            Debug.Log("===== 加载资源示例 =====");
            
            // 加载 UI
            var ui = await GridFramework.Resource.LoadAsync<GameObject>("Game/UI/Prefabs/main_menu");
            if (ui != null) Debug.Log("✅ UI 加载成功");
            
            // 加载音频
            var audio = await GridFramework.Resource.LoadAsync<AudioClip>("Game/Audio/Music/bgm_battle");
            if (audio != null) Debug.Log("✅ 音频加载成功");
            
            // 加载配置
            var config = await GridFramework.Resource.LoadAsync<TextAsset>("Game/Configs/game_config");
            if (config != null) Debug.Log("✅ 配置加载成功");
        }

        // 示例2：批量预加载
        async void ExamplePreload()
        {
            Debug.Log("===== 批量预加载示例 =====");
            
            await GridFramework.Resource.PreloadAsync(
                progress => Debug.Log($"加载进度: {progress * 100:F0}%"),
                "Game/UI/Prefabs/battle_hud",
                "Game/Audio/Music/bgm_battle",
                "Game/3D/Prefabs/Characters/player"
            );
            
            Debug.Log("✅ 批量预加载完成");
        }

        // 示例3：对象池
        async void ExampleObjectPool()
        {
            Debug.Log("===== 对象池示例 =====");
            
            // 加载预制体
            var bulletPrefab = await GridFramework.Resource.LoadAsync<GameObject>(
                "Game/Effects/Prefabs/projectile_bullet"
            );
            
            if (bulletPrefab != null)
            {
                // 创建对象池
                var pool = GridFramework.Pool.CreateGameObjectPool(bulletPrefab, minSize: 10);
                
                // 获取对象
                var bullet = pool.Spawn(Vector3.zero, Quaternion.identity);
                Debug.Log($"✅ 从对象池获取: {bullet.name}");
                
                // 归还对象
                pool.Despawn(bullet);
                Debug.Log($"✅ 归还到对象池");
            }
        }

        // 示例4：查看资源状态
        void ExampleResourceStatus()
        {
            Debug.Log("===== 资源状态查询 =====");
            Debug.Log($"缓存资源数: {GridFramework.Resource.CacheCount}");
            
            bool isLoaded = GridFramework.Resource.IsLoaded("Game/UI/Prefabs/main_menu");
            Debug.Log($"main_menu 是否已加载: {isLoaded}");
            
            int refCount = GridFramework.Resource.GetReferenceCount("Game/UI/Prefabs/main_menu");
            Debug.Log($"main_menu 引用计数: {refCount}");
        }
    }
}

