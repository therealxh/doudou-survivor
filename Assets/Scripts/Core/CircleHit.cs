using UnityEngine;

/// <summary>
/// 手算圆形碰撞（三件套③）：距离平方比较，避免开方；完全不依赖物理系统。
/// 本游戏所有单位高度固定，只比较水平面（XZ）距离。
/// </summary>
public static class CircleHit
{
    public static bool Hit(Vector3 a, float ra, Vector3 b, float rb)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        float r = ra + rb;
        return dx * dx + dz * dz <= r * r; // 平方比较：省一次开方
    }
}
