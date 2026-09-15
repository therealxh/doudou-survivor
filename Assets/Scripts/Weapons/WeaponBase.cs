using UnityEngine;

/// <summary>
/// 武器基类：统一管理冷却计时与自动索敌；具体开火行为由子类实现。
/// Day 2 最小版；Day 7 接入 WeaponManager（升级、候选池）。
/// </summary>
public abstract class WeaponBase : MonoBehaviour
{
    public string DisplayName = "武器";
    public int Level { get; private set; } = 1;
    public bool IsMaxed => Level >= 3;

    protected float Cooldown = 0.8f;
    protected float Range = 6f;
    protected float Damage = 10f;

    private float _timer;
    private Enemy _lockTarget; // 锁定目标：持续压制同一只，避免怪群推挤导致"每刀换人、谁都打不死"

    protected virtual void Update()
    {
        // 仅游玩状态推进冷却（升级时停/结算时自然冻结）
        if (GameManager.I == null || GameManager.I.State != GameState.Playing)
        {
            return;
        }

        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            _timer = Cooldown;
            Fire();
        }
    }

    /// <summary>子类实现：一次开火。</summary>
    protected abstract void Fire();

    /// <summary>
    /// 获取当前应攻击的目标：优先维持已锁定目标；
    /// 若锁定目标超出射程，改打“射程内的最近目标”（避免武器死等远处目标、看起来像哑火）；
    /// 换不到则保持锁定（等它跑回射程）。
    /// </summary>
    protected Enemy FindNearest()
    {
        var gm = GameManager.I;
        if (gm == null)
        {
            return null;
        }

        Enemy inRange = FindNearestInRange();

        if (IsValidTarget(_lockTarget))
        {
            float d = (_lockTarget.transform.position - transform.position).sqrMagnitude;
            if (d <= Range * Range)
            {
                return _lockTarget; // 锁定目标在射程内：维持压制
            }
            if (inRange != null)
            {
                _lockTarget = inRange; // 锁定目标超程：改打眼前的目标
                return inRange;
            }
            return _lockTarget; // 没有其他目标：保持锁定等它回来
        }

        _lockTarget = inRange;
        return inRange;
    }

    /// <summary>在射程内找最近目标（射程外开火是浪费弹药）。</summary>
    private Enemy FindNearestInRange()
    {
        Enemy best = null;
        float bestSqr = Range * Range;
        Vector3 pos = transform.position;
        var enemies = GameManager.I.Enemies;
        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (e == null)
            {
                continue;
            }
            float d = (e.transform.position - pos).sqrMagnitude;
            if (d < bestSqr)
            {
                bestSqr = d;
                best = e;
            }
        }
        return best;
    }

    private bool IsValidTarget(Enemy e)
    {
        if (e == null)
        {
            return false; // 已销毁
        }
        if (!e.gameObject.activeSelf)
        {
            return false; // 已被对象池回收
        }
        if (e.Hp <= 0f)
        {
            return false; // 已死亡
        }
        return true;
    }

    public void Upgrade()
    {
        Level++;
        OnUpgrade();
    }

    protected virtual void OnUpgrade() { }
}
