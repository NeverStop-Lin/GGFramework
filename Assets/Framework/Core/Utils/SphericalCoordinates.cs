using UnityEngine;

/// <summary>
/// 用于存储球坐标的结构体。
/// 注意：所有角度均使用弧度制。
/// </summary>
public struct Spherical
{
    public float radius;    // 半径 r
    public float theta;     // 极角 θ (与Y轴正方向的夹角, 0 到 PI)
    public float phi;       // 方位角 φ (绕Y轴的旋转, -PI 到 PI)

    public Spherical(float radius, float theta, float phi)
    {
        this.radius = radius;
        this.theta = theta;
        this.phi = phi;
    }

    /// <summary>
    /// 将当前球坐标转换为笛卡尔坐标 (Vector3)。
    /// </summary>
    public Vector3 ToCartesian()
    {
        return SphericalCoordinates.ToCartesian(this);
    }
}

/// <summary>
/// 提供笛卡尔坐标与球坐标之间相互转换的静态工具类。
/// Unity 坐标系：Y轴朝上。
/// </summary>
public static class SphericalCoordinates
{
    /// <summary>
    /// [反推] 将世界空间偏移（笛卡尔坐标）转换为球坐标。
    /// </summary>
    /// <param name="cartesian">世界空间中的一个点或偏移向量 (x, y, z)。</param>
    /// <returns>返回对应的球坐标。</returns>
    public static Spherical FromCartesian(Vector3 cartesian)
    {
        Spherical spherical = new Spherical();

        // 计算半径
        spherical.radius = cartesian.magnitude;

        // 防止除以零
        if (spherical.radius < 0.0001f)
        {
            return new Spherical(0f, 0f, 0f);
        }

        // 计算极角 (theta) - 与Y轴正方向的夹角
        // acos(y / r)
        spherical.theta = Mathf.Acos(Mathf.Clamp(cartesian.y / spherical.radius, -1f, 1f));

        // 计算方位角 (phi) - 绕Y轴的旋转角度
        // atan2(x, z) 可以正确处理所有象限
        spherical.phi = Mathf.Atan2(cartesian.x, cartesian.z);

        return spherical;
    }

    /// <summary>
    /// [转换] 将球坐标转换为世界空间偏移（笛卡尔坐标）。
    /// </summary>
    /// <param name="spherical">球坐标（半径，极角，方位角）。</param>
    /// <returns>返回对应的世界空间偏移 (Vector3)。</returns>
    public static Vector3 ToCartesian(Spherical spherical)
    {
        float x, y, z;

        // 从球坐标公式推导
        // y = r * cos(theta)
        y = spherical.radius * Mathf.Cos(spherical.theta);

        // 中间变量，计算在XZ平面上的投影长度
        float a = spherical.radius * Mathf.Sin(spherical.theta);

        // x = a * sin(phi)
        x = a * Mathf.Sin(spherical.phi);
        
        // z = a * cos(phi)
        z = a * Mathf.Cos(spherical.phi);

        return new Vector3(x, y, z);
    }
    
    /// <summary>
    /// [转换] 将球坐标转换为世界空间偏移（笛卡尔坐标）的重载版本。
    /// </summary>
    public static Vector3 ToCartesian(float radius, float theta, float phi)
    {
        return ToCartesian(new Spherical(radius, theta, phi));
    }
}