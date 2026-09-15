using UnityEngine;

/// <summary>
/// 回旋镖武器（Day 8 新武器）：朝最近敌人掷出回旋镖（飞出后折返、全程穿透）。
/// 升级：伤害 +3 ×3。
/// </summary>
public class WeaponBoomerang : WeaponBase
{
    private void Awake()
    {
        DisplayName = "回旋镖";
        Cooldown = 1.6f;
        Range = 7f;   // 最大飞行距离
        Damage = 8f;  // 单次穿透伤害（低于飞刀，但可打多只）
    }

    protected override void Fire()
    {
        var target = FindNearest();
        if (target == null)
        {
            return;
        }

        Vector3 toTarget = target.transform.position - transform.position;
        toTarget.y = 0f;
        float dist = toTarget.magnitude;
        Vector3 dir = dist < 0.0001f ? Vector3.forward : toTarget / dist;

        // 飞行距离：至少飞过目标一点，最多 Range
        float flyDist = Mathf.Min(Range, dist + 1f);

        Vector3 from = new Vector3(transform.position.x, 0.5f, transform.position.z);
        var b = GameBootstrap.BoomerangPool.Get();
        b.transform.position = from;
        b.transform.rotation = GameBootstrap.FlatRotation(dir);
        b.Launch(from, dir, flyDist, FinalDamage);
    }

    protected override void OnUpgrade()
    {
        Damage += 3f;
    }
}
