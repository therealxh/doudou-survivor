using UnityEngine;

/// <summary>
/// 飞刀：冷却到点后朝最近的敌人发射一枚投射物（子弹来自对象池）。
/// 升级：每级 +5 伤害。
/// </summary>
public class WeaponKnife : WeaponBase
{
    private void Awake()
    {
        DisplayName = "飞刀";
        Cooldown = 0.8f;
        Range = 6f;
        Damage = 12f; // 12 伤：开局两分钟内一刀一只（怪 HP 10×1.1^分钟，超 12 后变两刀）
    }

    protected override void Fire()
    {
        var target = FindNearest();
        if (target == null)
        {
            return;
        }

        // 朝目标方向（水平面）；用未归一化向量同时得到距离
        Vector3 toTarget = target.transform.position - transform.position;
        toTarget.y = 0f;
        float dist = toTarget.magnitude;

        // 锁定目标暂时超出射程（刚被打退/在靠近）：跳过这一刀，等它进入射程
        if (dist > Range)
        {
            return;
        }

        Vector3 dir;
        float offset; // 子弹出生偏移：贴脸时缩短，保证子弹生成在“玩家与目标之间”
        if (dist < 0.0001f)
        {
            dir = Vector3.forward; // 目标与玩家重合（方向退化）：从中心向任意方向发射，子弹就近必中
            offset = 0f;
        }
        else
        {
            dir = toTarget / dist;
            offset = Mathf.Min(0.7f, dist * 0.5f);
        }

        // 从池取子弹（Day 5 起：池 Get 替代 Instantiate）
        Vector3 spawnPos = new Vector3(transform.position.x, 0.5f, transform.position.z) + dir * offset;
        var proj = GameBootstrap.KnifePool.Get();
        proj.transform.position = spawnPos;
        proj.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        proj.speed = 12f;
        proj.range = Range;
        proj.Launch(dir, Damage);
    }

    protected override void OnUpgrade()
    {
        Damage += 5f;
    }
}
