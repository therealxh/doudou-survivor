using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 环绕飞刃武器（Day 8 新武器）：维持 Count 枚刀刃绕玩家旋转。
/// Fire = 定期检查并补足刀刃数量（新增的按均分相位入场）。
/// 升级：伤害 +2 ×3、数量 +1 ×2（上限 3）。
/// </summary>
public class WeaponOrbit : WeaponBase
{
    public int Count = 1; // 环绕刀刃数量

    private readonly List<Orbiter> _orbs = new List<Orbiter>();

    private void Awake()
    {
        DisplayName = "环绕飞刃";
        Cooldown = 0.5f;
        Range = 1.7f; // 环绕半径
        Damage = 3f;  // 每 0.4s 的扫击伤害
    }

    protected override void Fire()
    {
        // 清理已回收的
        for (int i = _orbs.Count - 1; i >= 0; i--)
        {
            if (_orbs[i] == null || !_orbs[i].gameObject.activeSelf)
            {
                _orbs.RemoveAt(i);
            }
        }

        // 补足数量（新增的按均分相位入场）
        while (_orbs.Count < Count)
        {
            var o = GameBootstrap.OrbitPool.Get();
            o.PhaseDeg = _orbs.Count * (360f / Mathf.Max(1, Count));
            o.ApplyPhase();
            _orbs.Add(o);
        }

        // 同步参数（升级即时生效）
        for (int i = 0; i < _orbs.Count; i++)
        {
            _orbs[i].DamagePerHit = FinalDamage;
            _orbs[i].OrbitRadius = Range;
        }
    }

    protected override void OnUpgrade()
    {
        Damage += 2f;
    }
}
