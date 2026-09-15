using UnityEngine;

/// <summary>
/// 飞刀：冷却到点后朝最近的敌人发射一枚投射物。
/// 升级（Day 8 前手动）：每级 +5 伤害。
/// </summary>
public class WeaponKnife : WeaponBase
{
    private void Awake()
    {
        DisplayName = "飞刀";
        Cooldown = 0.8f;
        Range = 6f;
        Damage = 10f;
    }

    protected override void Fire()
    {
        var target = FindNearest();
        if (target == null)
        {
            return;
        }

        // 朝目标方向（水平面）
        Vector3 dir = target.transform.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
        {
            return;
        }
        dir.Normalize();

        // Day 2 朴素版：Instantiate 模板（Day 5 换成对象池 Get）
        Vector3 spawnPos = new Vector3(transform.position.x, 0.5f, transform.position.z) + dir * 0.7f;
        var bullet = Instantiate(GameBootstrap.KnifeTemplate, spawnPos, Quaternion.LookRotation(dir, Vector3.up));
        bullet.SetActive(true);

        var proj = bullet.GetComponent<Projectile>();
        proj.speed = 12f;
        proj.range = Range;
        proj.Launch(dir, Damage);
    }

    protected override void OnUpgrade()
    {
        Damage += 5f;
    }
}
