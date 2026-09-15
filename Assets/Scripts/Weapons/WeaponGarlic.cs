using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 大蒜：周期性对自身周围范围内的所有敌人造成伤害（光环类武器）。
/// Day 5 修正包提前上线（开局随玩家携带；Day 7 升级系统上线后改为升级获取）。
/// 命中查询暂用全表遍历（Day 6 换空间哈希 Query）。
/// </summary>
public class WeaponGarlic : WeaponBase
{
    /// <summary>查询候选缓冲（静态共享：武器主线程串行执行）</summary>
    private static readonly List<Enemy> Candidates = new List<Enemy>();

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

        // 空间哈希邻近查询（Day 6 起）→ CircleHit 精筛
        gm.Grid.Query(pos, r + 0.45f, Candidates);
        int hitCount = 0;
        for (int i = 0; i < Candidates.Count; i++)
        {
            var e = Candidates[i];
            if (e == null || !e.gameObject.activeSelf)
            {
                continue; // 已回收（可能被前一次连锁击杀）
            }
            if (CircleHit.Hit(pos, r, e.transform.position, e.Radius))
            {
                // 不击退：大蒜若击退会把怪推出自己的光环（自相矛盾）；击退交给飞刀
                e.TakeDamage(FinalDamage, Vector3.zero);
                hitCount++;
            }
        }

        // 冲击波特效（Day 8：命中才播放，空场安静）
        if (hitCount > 0)
        {
            BlastEffect.ShowShockwave(pos, r);
        }
    }

    protected override void OnUpgrade()
    {
        Damage += 3f;
    }
}
