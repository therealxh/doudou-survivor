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
    /// 获取当前应攻击的目标：优先维持已锁定目标（活着且在射程内），
    /// 否则在射程内找最近目标并锁定。
    /// </summary>
    protected Enemy FindNearest()
    {
        var gm = GameManager.I;
        if (gm == null)
        {
            return null;
        }

        // 优先维持锁定：解决了"小怪群推挤、最近目标每刀切换→火力分散打不死"的问题
        if (IsValidTarget(_lockTarget))
        {
            return _lockTarget;
        }

        Enemy best = null;
        float bestSqr = Range * Range; // 只考虑射程内的目标（射程外开火是浪费弹药）
        Vector3 pos = transform.position;
        for (int i = 0; i < gm.Enemies.Count; i++)
        {
            var e = gm.Enemies[i];
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
        _lockTarget = best;
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
        // 刻意不检查距离：目标在追玩家，被击退后必然回到射程；
        // 以“距离”作失效条件会导致锁定在怪群边缘频繁切换→火力永远打在满血新怪上。
        return true;
    }

    public void Upgrade()
    {
        Level++;
        OnUpgrade();
    }

    protected virtual void OnUpgrade() { }
}
