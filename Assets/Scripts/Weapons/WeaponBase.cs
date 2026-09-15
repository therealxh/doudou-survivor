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

    /// <summary>索敌：遍历敌人名单找最近（每 0.8s 才一次，可忽略成本；命中检测才用空间哈希）。</summary>
    protected Enemy FindNearest()
    {
        var gm = GameManager.I;
        if (gm == null)
        {
            return null;
        }

        Enemy best = null;
        float bestSqr = float.MaxValue;
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
        return best;
    }

    public void Upgrade()
    {
        Level++;
        OnUpgrade();
    }

    protected virtual void OnUpgrade() { }
}
