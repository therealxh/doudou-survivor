using UnityEngine;

/// <summary>
/// 飞刀：冷却到点后朝最近的敌人发射（多重射击 = 扇形多发）；满 3 级伤害后可进化为弩枪。
/// 升级方式：LevelSystem 的升级池直接修改字段（伤害/冷却/多重）。
/// </summary>
public class WeaponKnife : WeaponBase
{
    public int MultiShot = 1;       // 每次发射的箭数（升级 +1，上限 3）
    public bool Evolved { get; private set; }

    private void Awake()
    {
        DisplayName = "飞刀";
        Cooldown = 0.8f;
        Range = 6f;
        Damage = 12f; // 12 伤：开局两分钟内一刀一只（怪 HP 10×1.1^分钟，超 12 后变两刀）
    }

    /// <summary>进化：弩枪（满 3 级伤害后解锁）——伤害/射程/间隔质变 + 弹体变大。</summary>
    public void Evolve()
    {
        Evolved = true;
        Damage += 10f;
        Range += 3f;
        Cooldown = Mathf.Max(0.3f, Cooldown - 0.1f);
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

        if (dist > Range)
        {
            return; // 锁定目标暂时超出射程（刚被打退/在靠近）：跳过这一刀
        }

        Vector3 dir;
        if (dist < 0.0001f)
        {
            dir = Vector3.forward; // 目标与玩家重合（方向退化）：从中心向任意方向发射，子弹就近必中
        }
        else
        {
            dir = toTarget / dist;
        }

        // 多重射击：以中心方向为轴扇形散布
        int n = Mathf.Max(1, MultiShot);
        for (int i = 0; i < n; i++)
        {
            float offsetAng = (i - (n - 1) * 0.5f) * 12f;
            Vector3 d = Quaternion.Euler(0f, offsetAng, 0f) * dir;
            FireOne(d, dist);
        }
    }

    private void FireOne(Vector3 dir, float dist)
    {
        // 出生偏移自适应：贴脸时缩短，保证子弹始终生成在"玩家与目标之间"
        float offset = Mathf.Min(0.7f, dist * 0.5f);
        Vector3 spawnPos = new Vector3(transform.position.x, 0.5f, transform.position.z) + dir * offset;

        var proj = GameBootstrap.KnifePool.Get();
        proj.transform.position = spawnPos;
        proj.transform.rotation = GameBootstrap.FlatRotation(dir);
        proj.speed = 12f;
        proj.range = Range;
        proj.Radius = Evolved ? 0.27f : 0.2f;             // 进化：命中半径同步放大
        proj.transform.localScale = Evolved ? Vector3.one * 1.35f : Vector3.one;

        proj.Launch(dir, FinalDamage);
    }

    protected override void OnUpgrade()
    {
        Damage += 5f;
    }
}
