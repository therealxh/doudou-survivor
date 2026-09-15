using UnityEngine;

/// <summary>
/// 手雷武器（Day 7）：定期朝最近的敌人投掷手雷（飞行体到达落点后范围爆炸）。
/// 获取方式：升级三选一（开局未携带）。
/// </summary>
public class WeaponGrenade : WeaponBase
{
    private void Awake()
    {
        DisplayName = "手雷";
        Cooldown = 3.5f; // Day 8 配平：2s → 3.5s（用户反馈投掷频率偏高）
        Range = 8f;
        Damage = 15f;
    }

    protected override void Fire()
    {
        var target = FindNearest();
        if (target == null)
        {
            return;
        }

        Vector3 from = new Vector3(transform.position.x, 0.5f, transform.position.z);
        Vector3 to = new Vector3(target.transform.position.x, 0.5f, target.transform.position.z);

        var g = GameBootstrap.GrenadePool.Get();
        g.transform.position = from;
        g.transform.rotation = GameBootstrap.FlatRotation(to - from);
        g.Launch(from, to, FinalDamage);
    }

    protected override void OnUpgrade()
    {
        Damage += 5f;
    }
}
