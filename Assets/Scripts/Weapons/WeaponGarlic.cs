using UnityEngine;

/// <summary>
/// 大蒜：周期性对自身周围范围内的所有敌人造成伤害（光环类武器）。
/// Day 5 修正包提前上线（开局随玩家携带；Day 7 升级系统上线后改为升级获取）。
/// 命中查询暂用全表遍历（Day 6 换空间哈希 Query）。
/// </summary>
public class WeaponGarlic : WeaponBase
{
    private void Awake()
    {
        DisplayName = "大蒜";
        Cooldown = 0.5f;
        Range = 2.5f;   // 光环半径
        Damage = 5f;
    }

    protected override void Fire()
    {
        var gm = GameManager.I;
        if (gm == null)
        {
            return;
        }

        Vector3 pos = transform.position;
        float r = Range;

        // 反向遍历：TakeDamage 可能触发击杀→回收→从名单移除
        var enemies = gm.Enemies;
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            var e = enemies[i];
            if (e == null)
            {
                continue;
            }
            if (CircleHit.Hit(pos, r, e.transform.position, e.Radius))
            {
                // 不击退：大蒜若击退会把怪推出自己的光环（自相矛盾）；击退交给飞刀
                e.TakeDamage(Damage, Vector3.zero);
            }
        }
    }

    protected override void OnUpgrade()
    {
        Damage += 3f;
    }
}
