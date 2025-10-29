using UnityEngine;

namespace 游戏应用.角色
{
    /// <summary>
    /// 角色控制器参数配置（ScriptableObject）
    /// </summary>
    [CreateAssetMenu(fileName = "默认角色配置", menuName = "GameApp/角色/角色控制器配置")]
    public class 角色控制器配置 : ScriptableObject
    {
        [Header("基础移动")]
        public float 最大稳定移动速度 = 6f;
        public float 稳定移动锐度 = 15f;
        public float 方向插值锐度 = 12f;

        [Header("空中移动")]
        public float 最大空中移动速度 = 8f;
        public float 空中加速度 = 20f;
        public float 空中阻力 = 0.1f;
        public Vector3 重力 = new Vector3(0, -30f, 0);

        [Header("跳跃")]
        public bool 斜坡可跳 = false;
        public float 跳起速度 = 10f;
        public float 跳跃前向速度 = 8f;
        public int 最大跳跃次数 = 2;
        public float 土狼时间 = 0.1f; // 离地后仍可跳的宽限
        public float 跳跃缓冲时间 = 0.1f; // 提前按下跳跃的宽限

        [Header("飞扑")]
        public float 飞扑初速度 = 12f;
        public float 飞扑持续时间 = 0.25f;
        public float 飞扑衰减 = 12f;
        public float 飞扑冷却时间 = 0.5f;
        public bool 空中飞扑消耗一次跳跃 = true;

        [Header("趴下/蹲伏")]
        public float 蹲伏身高 = 1.0f;
        public float 站立身高 = 2.0f;
        public float 胶囊半径 = 0.5f;
        public float 蹲伏移动速度倍率 = 0.5f;

        [Header("爬梯子")]
        public float 梯子检测半径 = 0.4f;
        public float 梯子检测距离 = 0.8f;
        public float 爬梯子速度 = 4f;
        public string 梯子Tag = "Ladder";
        public LayerMask 梯子层;

        [Header("爬墙/登墙跳")]
        public LayerMask 可爬墙层;
        public float 可爬墙最大表面角度 = 75f;
        public float 贴墙检测距离 = 0.6f;
        public float 下滑速度 = 2.5f;
        public float 抓墙最久时间 = 3.0f;
        public float 登墙跳上升速度 = 10f;
        public float 登墙跳后退速度 = 8f;
    }
}



