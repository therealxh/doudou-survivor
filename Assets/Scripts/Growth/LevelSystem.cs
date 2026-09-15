using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>升级选项：标题 + 描述 + 应用逻辑（由 LevelSystem 生成）。</summary>
public class UpgradeOption
{
    public string Title;
    public string Desc;
    public Action Apply;
}

/// <summary>
/// 成长系统：经验 / 等级 / 升级三选一（Day 7）。
/// 升级池：武器升级维度（伤害/射速/多重/进化）+ 获得武器（大蒜/手雷）+ 4 种被动。
/// 升级生效方式 = 直接修改武器/玩家的字段（最小复杂度，便于小白重做）。
/// </summary>
public class LevelSystem : MonoBehaviour
{
    public int Level { get; private set; } = 1;
    public float Exp { get; private set; }
    public float ExpToNext => 12f + (Level - 1) * 8f; // Day 8 配平：升级节奏拉长（用户反馈升级太快）

    private PlayerStats _stats;
    private PlayerController _pc;
    private WeaponKnife _knife;
    private WeaponGarlic _garlic;
    private WeaponGrenade _grenade;
    private WeaponOrbit _orbit;         // Day 8 新武器（未获得 = null）
    private WeaponBoomerang _boom;      // Day 8 新武器（未获得 = null）

    // —— 升级项等级（限上限 + 面板计数） ——
    private int _knifeDmg, _knifeSpeed, _knifeMulti;
    private bool _evolved;
    private int _garlicDmg, _garlicRadius;
    private int _grenadeDmg;
    private int _orbitDmg, _orbitCount, _orbitSpeed;
    private int _boomDmg;
    private int _passAtk, _passMove, _passHp, _passPick;

    private readonly List<UpgradeOption> _pool = new List<UpgradeOption>(16);
    private List<UpgradeOption> _choices;

    public IReadOnlyList<UpgradeOption> Choices => _choices;

    private void Start()
    {
        var p = GameBootstrap.Player;
        if (p == null)
        {
            return;
        }
        _stats = p.GetComponent<PlayerStats>();
        _pc = p.GetComponent<PlayerController>();
        _knife = p.GetComponent<WeaponKnife>();
        _garlic = p.GetComponent<WeaponGarlic>();   // 开局未获得 → null
        _grenade = p.GetComponent<WeaponGrenade>(); // 开局未获得 → null
        _orbit = p.GetComponent<WeaponOrbit>();     // 开局未获得 → null
        _boom = p.GetComponent<WeaponBoomerang>();  // 开局未获得 → null
    }

    /// <summary>拾取宝石时调用：满经验则升级并弹出三选一。</summary>
    public void AddExp(float v)
    {
        Exp += v;
        if (Exp < ExpToNext)
        {
            return;
        }
        Exp -= ExpToNext;
        Level++;
        OpenLevelUp();
    }

    /// <summary>面板点击后调用：应用选项并恢复游戏。</summary>
    public void Pick(int index)
    {
        if (_choices == null || index < 0 || index >= _choices.Count)
        {
            return;
        }
        _choices[index].Apply();
        _choices = null;

        if (LevelUpPanel.I != null)
        {
            LevelUpPanel.I.Hide();
        }
        GameManager.I.Resume();
    }

    private void OpenLevelUp()
    {
        BuildPool();
        _choices = new List<UpgradeOption>(3);

        // 等权随机 3 个不重复
        while (_choices.Count < 3 && _pool.Count > 0)
        {
            int i = UnityEngine.Random.Range(0, _pool.Count);
            _choices.Add(_pool[i]);
            _pool.RemoveAt(i);
        }

        GameManager.I.EnterLevelUp();
        if (LevelUpPanel.I != null)
        {
            LevelUpPanel.I.Show(_choices);
        }
    }

    private void BuildPool()
    {
        _pool.Clear();

        // —— 飞刀（开局武器） ——
        if (_knife != null)
        {
            if (_knifeDmg < 3)
            {
                _pool.Add(new UpgradeOption
                {
                    Title = "飞刀 · 伤害 +3",
                    Desc = "弩箭伤害提升（当前等级 " + _knifeDmg + "/3）",
                    Apply = () => { _knifeDmg++; _knife.AddDamage(3f); },
                });
            }
            if (_knifeSpeed < 3)
            {
                _pool.Add(new UpgradeOption
                {
                    Title = "飞刀 · 射速 +",
                    Desc = "攻击间隔 -0.1s（当前等级 " + _knifeSpeed + "/3）",
                    Apply = () => { _knifeSpeed++; _knife.AddCooldown(0.1f); },
                });
            }
            if (_knifeMulti < 2)
            {
                _pool.Add(new UpgradeOption
                {
                    Title = "飞刀 · 多重射击 +1",
                    Desc = "一次射出更多弩箭（当前等级 " + _knifeMulti + "/2）",
                    Apply = () => { _knifeMulti++; _knife.MultiShot++; },
                });
            }
            if (_knifeDmg >= 3 && !_evolved)
            {
                _pool.Add(new UpgradeOption
                {
                    Title = "★ 进化：弩枪",
                    Desc = "伤害 +10 / 射程 +3m / 间隔 -0.1s / 弹体变大",
                    Apply = () => { _evolved = true; _knife.Evolve(); },
                });
            }
        }

        // —— 大蒜（升级获取） ——
        if (_garlic == null)
        {
            _pool.Add(new UpgradeOption
            {
                Title = "获得武器：大蒜",
                Desc = "周身光环，持续伤害靠近的敌人",
                Apply = () => { _garlic = GameBootstrap.Player.AddComponent<WeaponGarlic>(); },
            });
        }
        else
        {
            if (_garlicDmg < 3)
            {
                _pool.Add(new UpgradeOption
                {
                    Title = "大蒜 · 伤害 +2",
                    Desc = "光环伤害提升（当前等级 " + _garlicDmg + "/3）",
                    Apply = () => { _garlicDmg++; _garlic.AddDamage(2f); },
                });
            }
            if (_garlicRadius < 2)
            {
                _pool.Add(new UpgradeOption
                {
                    Title = "大蒜 · 半径 +0.5m",
                    Desc = "光环覆盖更广（当前等级 " + _garlicRadius + "/2）",
                    Apply = () => { _garlicRadius++; _garlic.AddRange(0.5f); },
                });
            }
        }

        // —— 手雷（升级获取） ——
        if (_grenade == null)
        {
            _pool.Add(new UpgradeOption
            {
                Title = "获得武器：手雷",
                Desc = "定期向敌人投掷，落点范围爆炸",
                Apply = () => { _grenade = GameBootstrap.Player.AddComponent<WeaponGrenade>(); },
            });
        }
        else if (_grenadeDmg < 3)
        {
            _pool.Add(new UpgradeOption
            {
                Title = "手雷 · 伤害 +3",
                Desc = "爆炸伤害提升（当前等级 " + _grenadeDmg + "/3）",
                Apply = () => { _grenadeDmg++; _grenade.AddDamage(3f); },
            });
        }

        // —— 环绕飞刃（Day 8 新武器，升级获取） ——
        if (_orbit == null)
        {
            _pool.Add(new UpgradeOption
            {
                Title = "获得武器：环绕飞刃",
                Desc = "刀刃环绕身周，持续割伤靠近的敌人",
                Apply = () => { _orbit = GameBootstrap.Player.AddComponent<WeaponOrbit>(); },
            });
        }
        else
        {
            if (_orbitDmg < 3)
            {
                _pool.Add(new UpgradeOption
                {
                    Title = "环绕飞刃 · 伤害 +2",
                    Desc = "扫击伤害提升（当前等级 " + _orbitDmg + "/3）",
                    Apply = () => { _orbitDmg++; _orbit.AddDamage(2f); },
                });
            }
            if (_orbitCount < 2)
            {
                _pool.Add(new UpgradeOption
                {
                    Title = "环绕飞刃 · 数量 +1",
                    Desc = "环绕刀刃更多（当前等级 " + _orbitCount + "/2）",
                    Apply = () => { _orbitCount++; _orbit.Count++; },
                });
            }
            if (_orbitSpeed < 3)
            {
                _pool.Add(new UpgradeOption
                {
                    Title = "环绕飞刃 · 转速 +25%",
                    Desc = "刀刃旋转更快（当前等级 " + _orbitSpeed + "/3）",
                    Apply = () => { _orbitSpeed++; _orbit.SpeedMult += 0.25f; },
                });
            }
        }

        // —— 回旋镖（Day 8 新武器，升级获取） ——
        if (_boom == null)
        {
            _pool.Add(new UpgradeOption
            {
                Title = "获得武器：回旋镖",
                Desc = "掷出回旋镖，飞出折返、全程穿透",
                Apply = () => { _boom = GameBootstrap.Player.AddComponent<WeaponBoomerang>(); },
            });
        }
        else if (_boomDmg < 3)
        {
            _pool.Add(new UpgradeOption
            {
                Title = "回旋镖 · 伤害 +3",
                Desc = "穿透伤害提升（当前等级 " + _boomDmg + "/3）",
                Apply = () => { _boomDmg++; _boom.AddDamage(3f); },
            });
        }

        // —— 被动（各 3 级） ——
        if (_passAtk < 3)
        {
            _pool.Add(new UpgradeOption
            {
                Title = "强化 · 攻击力 +10%",
                Desc = "所有武器伤害提升（当前等级 " + _passAtk + "/3）",
                Apply = () => { _passAtk++; _stats.AttackMult += 0.1f; },
            });
        }
        if (_passMove < 3)
        {
            _pool.Add(new UpgradeOption
            {
                Title = "强化 · 移速 +10%",
                Desc = "移动更快（当前等级 " + _passMove + "/3）",
                Apply = () => { _passMove++; _pc.MoveSpeed *= 1.1f; },
            });
        }
        if (_passHp < 3)
        {
            _pool.Add(new UpgradeOption
            {
                Title = "强化 · 生命 +20",
                Desc = "上限提升并立即回复（当前等级 " + _passHp + "/3）",
                Apply = () => { _passHp++; _stats.MaxHp += 20f; _stats.Heal(20f); },
            });
        }
        if (_passPick < 3)
        {
            _pool.Add(new UpgradeOption
            {
                Title = "强化 · 拾取范围 +20%",
                Desc = "宝石吸附更远（当前等级 " + _passPick + "/3）",
                Apply = () => { _passPick++; _stats.PickupMult += 0.2f; },
            });
        }
    }
}
